using System.Linq;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Util;
using Android.Views;
using Com.Androidadvance.Topsnackbar;
using CrossBreed.Chat;
using CrossBreed.ViewModels;
using ML.AppBase.Droid;
using MvvmCross.Binding.Droid.BindingContext;
using MvvmCross.Binding.Droid.Views;
using MvvmCross.Droid.Shared.Attributes;
using MvvmCross.Droid.Support.V4;

namespace CrossBreed.Droid {
	[MvxFragment(typeof(MainViewModel), Resource.Id.content_frame, true)]
	[Register("crossbreed.droid.PeopleFragment")]
	public class PeopleFragment : MvxFragment<TabbedPeopleViewModel> {
		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			base.OnCreateView(inflater, container, savedInstanceState);
			HasOptionsMenu = true;
			ViewModel.PropertyChanged += (sender, args) => {
				if(args.PropertyName == nameof(ViewModel.SelectedCharacter)) Activity.InvalidateOptionsMenu();
			};
			var view = this.BindingInflate(Resource.Layout.fragment_people, container, false);
			((MainActivity)Activity).ToastSent += toast => {
				var bar = TSnackbar.Make(view.FindViewById(Android.Resource.Id.Content), toast.Text, TSnackbar.LengthLong);
				if(toast.Action != null) bar.SetAction(toast.ActionText, toast.Action);
				bar.Show();
			};
			var listView = view.FindViewById<MvxListView>(Resource.Id.list);
			var tabLayout = view.FindViewById<TabLayout>(Resource.Id.tabs);
			var tabs = ViewModel.Tabs.ToDictionary(x => tabLayout.NewTab().SetText(x.Name));
			foreach(var tab in tabs.Keys) tabLayout.AddTab(tab);
			tabLayout.TabSelected += (sender, args) => {
				ViewModel.TabSelectedCommand.Execute(tabs[args.Tab]);
				listView.ItemTemplateId = args.Tab.Position == 2 ? Resource.Layout.list_item_character : Resource.Layout.list_item_character_image;
			};
			var value = new TypedValue();
			Context.Theme.ResolveAttribute(Resource.Attribute.colorAccent, value, true);
			listView.Selector = new ColorDrawable(new Color(value.Data));
			return view;
		}

		public override bool OnOptionsItemSelected(IMenuItem item) {
			switch(item.ItemId) {
				case Resource.Id.chat:
					ViewModel.SelectedCharacter.MessageCommand.Execute();
					return true;
				case Resource.Id.profile:
					ViewModel.SelectedCharacter.ShowProfileCommand.Execute();
					return true;
				case 1:
					ViewModel.SelectedCharacter.KickCommand.Execute();
					return true;
				case 2:
					ViewModel.SelectedCharacter.BanCommand.Execute();
					return true;
				case 3:
					ViewModel.SelectedCharacter.ToggleOpCommand.Execute();
					return true;
				default:
					return base.OnOptionsItemSelected(item);
			}
		}

		public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater) {
			base.OnCreateOptionsMenu(menu, inflater);
			var character = ViewModel.SelectedCharacter;
			if(character == null) return;
			if(character.AdminActionsAvailable) {
				menu.Add(Menu.None, 1, Menu.None, Strings.Character_Kick);
				menu.Add(Menu.None, 2, Menu.None, Strings.Character_Ban);
				menu.Add(Menu.None, 3, Menu.None, character.ToggleOpCommandName);
			}
			inflater.LocalizedInflate(Resource.Menu.people, menu);
		}
	}
}