using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using CrossBreed.Chat;
using CrossBreed.ViewModels;
using ML.AppBase.Droid;

namespace CrossBreed.Droid {
	[Activity]
	public class ChannelMembersActivity : BaseActivity<ChannelMembersViewModel> {
		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);
			Title = ViewModel.Channel.Name;
			SupportActionBar.SetDisplayHomeAsUpEnabled(true);
			ViewModel.SelectedMemberChanged += InvalidateOptionsMenu;
			SetContentView(Resource.Layout.activity_channel_members);
			var value = new TypedValue();
			Theme.ResolveAttribute(Resource.Attribute.colorAccent, value, true);
			FindViewById<ListView>(Resource.Id.list).Selector = new ColorDrawable(new Color(value.Data));
		}

		public override bool OnOptionsItemSelected(IMenuItem item) {
			switch(item.ItemId) {
				case Resource.Id.chat:
					ViewModel.SelectedMember.Character.MessageCommand.Execute();
					return true;
				case Resource.Id.profile:
					ViewModel.SelectedMember.Character.ShowProfileCommand.Execute();
					return true;
				case 1:
					ViewModel.SelectedMember.ChannelKickCommand.Execute();
					return true;
				case 2:
					ViewModel.SelectedMember.ChannelBanCommand.Execute();
					return true;
				case 3:
					ViewModel.SelectedMember.ChannelToggleOpCommand.Execute();
					return true;
				default:
					return base.OnOptionsItemSelected(item);
			}
		}

		public override bool OnCreateOptionsMenu(IMenu menu) {
			base.OnCreateOptionsMenu(menu);
			if(ViewModel.SelectedMember == null) return true;
			MenuInflater.LocalizedInflate(Resource.Menu.channel_members, menu);
			if(ViewModel.SelectedMember.ChannelAdminActionsAvailable) {
				menu.Add(Menu.None, 1, Menu.None, Strings.Channel_Kick);
				menu.Add(Menu.None, 2, Menu.None, Strings.Channel_Ban);
				menu.Add(Menu.None, 3, Menu.None, ViewModel.SelectedMember.ChannelToggleOpCommandName);
			}
			return true;
		}
	}
}