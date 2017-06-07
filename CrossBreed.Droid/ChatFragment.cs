using System;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Text.Method;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Com.Androidadvance.Topsnackbar;
using CrossBreed.Chat;
using CrossBreed.ViewModels;
using ML.AppBase.Droid;
using MvvmCross.Binding.Droid.BindingContext;
using MvvmCross.Binding.Droid.Views;
using MvvmCross.Droid.Shared.Attributes;
using MvvmCross.Droid.Support.V4;
using MvvmCross.Droid.Support.V7.RecyclerView;

namespace CrossBreed.Droid {
	[MvxFragment(typeof(MainViewModel), Resource.Id.content_frame, true)]
	[Register("crossbreed.droid.ChatFragment")]
	public class ChatFragment : MvxFragment<ChatViewModel> {
		private RecyclerView tabView;

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			base.OnCreateView(inflater, container, savedInstanceState);
			HasOptionsMenu = true;
			var view = this.BindingInflate(Resource.Layout.fragment_chat, container, false);
			((MainActivity) Activity).ToastSent += toast => {
				var bar = TSnackbar.Make(view.FindViewById(Android.Resource.Id.Content), toast.Text, TSnackbar.LengthLong);
				bar.View.SetBackgroundColor(Color.Gray);
				if(toast.Action != null) bar.SetAction(toast.ActionText, toast.Action);
				bar.Show();
			};
			tabView = view.FindViewById<RecyclerView>(Resource.Id.channelTabs);
			tabView.SetLayoutManager(new LinearLayoutManager(Context, LinearLayoutManager.Horizontal, false));
			if(ViewModel.SelectedTab != null) ScrollToTab();

			var messageList = view.FindViewById<MvxListView>(Resource.Id.messageList);
			messageList.Adapter = new MessageAdapter(Context, (IMvxAndroidBindingContext) BindingContext, () => messageList.Post(() => {
				if(messageList.ChildCount == 0) return;
				if(messageList.LastVisiblePosition == messageList.Adapter.Count - 2 && messageList.GetChildAt(messageList.ChildCount - 1).Bottom <= messageList.Height) {
					messageList.SetSelection(messageList.Adapter.Count - 1);
				}
			}));

			ViewModel.TabSelected += () => {
				ScrollToTab();
				messageList.SetSelection(messageList.Count - 1);
				Activity.InvalidateOptionsMenu();
			};
			RegisterForContextMenu(view.FindViewById<Button>(Resource.Id.sendButton));
			view.FindViewById<EditText>(Resource.Id.input).EditorAction += (sender, args) => {
				var enter = args.Event != null && args.Event.DeviceId > 0 && (args.Event.Action == KeyEventActions.Down && args.Event.KeyCode == Keycode.Enter || !args.Event.IsShiftPressed);
				if(args.ActionId == ImeAction.Send || args.ActionId == ImeAction.Done || enter) {
					if(ViewModel.SelectedTab.SendCommand.CanExecute()) ViewModel.SelectedTab.SendCommand.Execute();
					args.Handled = true;
				}
			};
			return view;
		}

		private void ScrollToTab() {
			var tab = ViewModel.SelectedTab;
			var source = ((IMvxRecyclerAdapter) tabView.GetAdapter()).ItemsSource?.GetEnumerator();
			if(source == null) return;
			for(var i = 0; source.MoveNext(); ++i) {
				if(source.Current != tab) continue;
				tabView.ScrollToPosition(i);
				break;
			}
		}

		public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater) {
			base.OnCreateOptionsMenu(menu, inflater);
			var channelTab = ViewModel.SelectedTab as ChannelConversationViewModel;
			if(channelTab != null) {
				inflater.LocalizedInflate(Resource.Menu.chat_channel, menu);
				if(channelTab.ToggleAdsCommand.CanExecute()) menu.Add(Menu.None, 1, 1, channelTab.ToggleAdsCommandName); //TODO
			} else if(ViewModel.SelectedTab is CharacterConversationViewModel) inflater.LocalizedInflate(Resource.Menu.chat_character, menu);
			menu.Add(Menu.None, 2, 1, Strings.Chat_SaveChannels); //TODO
		}

		public override void OnCreateContextMenu(IContextMenu menu, View view, IContextMenuContextMenuInfo menuInfo) {
			base.OnCreateContextMenu(menu, view, menuInfo);
			if(ViewModel.SelectedTab is ConversationViewModel) {
				menu.Add(Menu.None, 1, Menu.None, Strings.Chat_Preview);
				var channel = ViewModel.SelectedTab as ChannelConversationViewModel;
				if(channel != null && channel.IsShowingAds) { //TODO
					menu.Add(Menu.None, 2, Menu.None, Strings.Chat_SendAdAuto);
				}
			}
		}

		public override bool OnContextItemSelected(IMenuItem item) {
			switch(item.ItemId) {
				case 1:
					ShowPreview();
					break;
				case 2:
					//((ChannelConversationViewModel) ViewModel.SelectedTab).SendAdCommand.Execute(true); TODO
					break;
			}
			return base.OnContextItemSelected(item);
		}

		private async void ShowPreview() {
			var preview = await BBCodeBinding.GetFormatted(((ConversationViewModel) ViewModel.SelectedTab).GetPreview());
			new AlertDialog.Builder(Activity).SetMessage(preview).Show();
		}

		public override bool OnOptionsItemSelected(IMenuItem item) {
			switch(item.ItemId) {
				case Resource.Id.profile:
					((CharacterConversationViewModel) ViewModel.SelectedTab).Character.ShowProfileCommand.Execute(null);
					break;
				case Resource.Id.people:
					((ChannelConversationViewModel) ViewModel.SelectedTab).ShowMembersCommand.Execute(null);
					break;
				case Resource.Id.leave:
					((ConversationViewModel) ViewModel.SelectedTab).CloseCommand.Execute(null);
					break;
				case 1:
					((ChannelConversationViewModel) ViewModel.SelectedTab).ToggleAdsCommand.Execute();
					Activity.InvalidateOptionsMenu();
					break;
				case 2:
					((ConversationViewModel) ViewModel.SelectedTab).TogglePinCommand.Execute(null);
					break;
			}
			return base.OnOptionsItemSelected(item);
		}

		private sealed class MessageAdapter : MvxAdapter<Message> {
			private readonly Action onDataSetChanged;

			public MessageAdapter(Context context, IMvxAndroidBindingContext bindingContext, Action onDataSetChanged) : base(context, bindingContext) {
				this.onDataSetChanged = onDataSetChanged;
				ItemTemplateId = Resource.Layout.list_item_message;
			}

			public MessageAdapter(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {}

			protected override IMvxListItemView CreateBindableView(object dataContext, int templateId) {
				var view = base.CreateBindableView(dataContext, templateId);
				((TextView) ((ViewGroup) view).GetChildAt(0)).MovementMethod = LinkMovementMethod.Instance;
				return view;
			}

			protected override void RealNotifyDataSetChanged() {
				base.RealNotifyDataSetChanged();
				onDataSetChanged();
			}
		}
	}
}