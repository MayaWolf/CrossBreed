using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Text.Method;
using Android.Util;
using Android.Views;
using Android.Widget;
using CodeKicker.BBCode.SyntaxTree;
using CrossBreed.Chat;
using CrossBreed.ViewModels;
using CrossBreed.Entities;
using Java.Lang;
using ML.AppBase.Droid;
using ML.Droid.Base;
using MvvmCross.Binding.Droid.BindingContext;
using MvvmCross.Droid.Support.V4;
using MvvmCross.Platform;
using MvvmCross.Platform.IoC;
using MvvmCross.Plugins.DownloadCache;
using Fragment = Android.Support.V4.App.Fragment;
using Uri = Android.Net.Uri;

namespace CrossBreed.Droid {
	[Activity]
	public class ProfileActivity : BaseActivity<CharacterViewModel> {
		protected override void OnViewModelSet() {
			base.OnViewModelSet();
			Title = ViewModel.Character.Name;
			SupportActionBar.DisplayOptions |= Android.Support.V7.App.ActionBar.DisplayHomeAsUp | Android.Support.V7.App.ActionBar.DisplayShowHome;
			SetContentView(Resource.Layout.activity_profile);
			var pager = FindViewById<ViewPager>(Resource.Id.pager);
			pager.Adapter = new Adapter(this);
			FindViewById<TabLayout>(Resource.Id.tabs).SetupWithViewPager(pager);
			ViewModel.Character.IsBookmarkedChanged += InvalidateOptionsMenu;
			ViewModel.Character.IsIgnoredChanged += InvalidateOptionsMenu;
		}

		protected override void OnDestroy() {
			ViewModel.Character.IsBookmarkedChanged -= InvalidateOptionsMenu;
			ViewModel.Character.IsIgnoredChanged -= InvalidateOptionsMenu;
			base.OnDestroy();
		}

		public override bool OnOptionsItemSelected(IMenuItem item) {
			switch(item.ItemId) {
				case Android.Resource.Id.Home:
					OnBackPressed();
					break;
				case 1:
					ViewModel.ToggleBookmarkCommand.Execute(null);
					break;
				case 2:
					ViewModel.ToggleIgnoreCommand.Execute(null);
					break;
				case 3:
					StartActivity(new Intent(Intent.ActionView, Uri.Parse("https://www.f-list.net/c/" + ViewModel.Character.Name)));
					break;
			}
			return base.OnOptionsItemSelected(item);
		}

		public override bool OnCreateOptionsMenu(IMenu menu) {
			menu.Add(Menu.None, 1, Menu.None, ViewModel.ToggleBookmarkCommandName);
			menu.Add(Menu.None, 2, Menu.None, ViewModel.ToggleIgnoreCommandName);
			menu.Add(Menu.None, 3, Menu.None, Strings.Profile_OpenInBrowser);
			return base.OnCreateOptionsMenu(menu);
		}

		private class Adapter : FragmentPagerAdapter {
			private readonly ProfileActivity activity;

			private static readonly string[] titles = { Strings.Profile_Profile, Strings.Profile_Info, Strings.Profile_Kinks, Strings.Profile_Friends };

			public override int Count => 4;
			public Adapter(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {}

			public Adapter(ProfileActivity activity) : base(activity.SupportFragmentManager) {
				this.activity = activity;
			}

			public override Fragment GetItem(int position) {
				switch(position) {
					case 0:
						return new ProfileFragment { ViewModel = activity.ViewModel };
					case 1:
						return new InfoFragment { ViewModel = activity.ViewModel };
					case 2:
						return new KinksFragment { ViewModel = activity.ViewModel };
					case 3:
						return new FriendsFragment { ViewModel = activity.ViewModel };
					default:
						throw new IndexOutOfRangeException();
				}
			}

			public override ICharSequence GetPageTitleFormatted(int position) => new Java.Lang.String(titles[position]);
		}

		[MvxUnconventional]
		private class ProfileFragment : MvxFragment<CharacterViewModel> {
			private static readonly LinearLayout.LayoutParams defaultLayoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
			private static readonly HashSet<string> layoutTags = new HashSet<string> { "indent", "collapse", "quote", "hr", "left", "center", "right", "heading", "img" };

			public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
				base.OnCreateView(inflater, container, savedInstanceState);
				var view = this.BindingInflate(Resource.Layout.fragment_profile, container, false);
				ViewModel.GetProfile().ContinueWith(async task => {
					var parent = view.FindViewById<LinearLayout>(Resource.Id.profile);
					var profileView = await CreateProfileView(task.Result);
					Activity.RunOnUiThread(() => {
						parent.RemoveViewAt(1);
						parent.AddView(profileView);
					});
				});
				return view;
			}

			private async Task<View> CreateProfileView(ProfileViewModel profile) {
				var parent = new LinearLayout(Activity) { LayoutParameters = defaultLayoutParams, Orientation = Orientation.Vertical };
				var images = Mvx.GetSingleton<IMvxImageCache<Bitmap>>();
				var inlines = new Dictionary<int, Bitmap>(profile.InlineImages.Count);
				foreach(var inline in profile.InlineImages) inlines.Add(inline.Key, await images.RequestImage(inline.Value.Url));

				var text = await BBCodeBinding.GetFormatted(profile.Description, (node, builder) => AddProfile(builder, node, inlines, parent, 0));
				AddTextView(text, parent, 0);
				return parent;
			}

			private async Task<bool> AddProfile(SpannableStringBuilder builder, TagNode node, IDictionary<int, Bitmap> inlines, LinearLayout parent, GravityFlags gravity) {
				if(!layoutTags.Contains(node.Tag.Name)) return false;
				AddTextView(builder, parent, gravity);
				var parameter = node.AttributeValues.Count > 0 ? node.AttributeValues.Values.First() : null;
				LinearLayout newParent;
				switch(node.Tag.Name) {
					case "collapse":
						newParent = new CollapsingView(Activity) { LayoutParameters = defaultLayoutParams, Title = parameter };
						break;
					case "indent":
						newParent = new LinearLayout(Activity) { LayoutParameters = defaultLayoutParams, Orientation = Orientation.Vertical };
						newParent.SetPaddingRelative(MLHelpers.DpToPixelsInt(10), 0, 0, 0);
						break;
					case "left":
					case "center":
					case "right":
						newParent = new LinearLayout(Activity) { LayoutParameters = defaultLayoutParams, Orientation = Orientation.Vertical };
						gravity = node.Tag.Name == "center" ? GravityFlags.Center : node.Tag.Name == "right" ? GravityFlags.End : GravityFlags.Start;
						newParent.SetHorizontalGravity(gravity);
						break;
					case "hr":
						var value = new TypedValue();
						Activity.Theme.ResolveAttribute(Resource.Attribute.colorAccent, value, true);
						var hr = new View(Activity) { LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, MLHelpers.DpToPixelsInt(2)) };
						hr.SetBackgroundColor(new Color(value.Data));
						parent.AddView(hr);
						return true;
					case "heading":
						var heading = new TextView(Activity) {
							LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) {
								BottomMargin = MLHelpers.DpToPixelsInt(5)
							},
							Text = node.ToText()
						};
						heading.SetTextSize(ComplexUnitType.Sp, 22);
						parent.AddView(heading);
						return true;
					case "img":
						var image = new ImageView(Activity) {
							LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
						};
						image.SetImageBitmap(inlines[int.Parse(parameter)]);
						parent.AddView(image);
						return true;
					case "quote":
						newParent = new LinearLayout(Activity) { LayoutParameters = defaultLayoutParams, Orientation = Orientation.Vertical };
						var padding = MLHelpers.DpToPixelsInt(5);
						newParent.SetPadding(padding, padding, padding, padding);
						newParent.SetBackgroundResource(Resource.Drawable.background_quote);
						var text = new TextView(Activity) {
							LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) {
								BottomMargin = MLHelpers.DpToPixelsInt(5)
							},
							Text = Strings.Profile_Quote
						};
						text.SetTypeface(null, TypefaceStyle.Bold);
						newParent.AddView(text);
						return true;
					default:
						return false;
				}
				parent.AddView(newParent);
				var remaining = await BBCodeBinding.GetFormatted(node, (n, b) => AddProfile(b, n, inlines, newParent, gravity));
				AddTextView(remaining, newParent, gravity);
				return true;
			}

			private void AddTextView(SpannableStringBuilder builder, LinearLayout parent, GravityFlags gravity) {
				if(builder.Length() == 0) return;
				parent.AddView(new TextView(Activity) {
					LayoutParameters = defaultLayoutParams,
					TextFormatted = builder,
					Gravity = gravity,
					MovementMethod = LinkMovementMethod.Instance
				});
				builder.Clear();
			}
		}

		[MvxUnconventional]
		private class InfoFragment : MvxFragment<CharacterViewModel> {
			public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
				base.OnCreateView(inflater, container, savedInstanceState);

				var view = new RecyclerView(Activity);
				view.SetLayoutManager(new LinearLayoutManager(Activity, LinearLayoutManager.Vertical, false));
				ViewModel.GetProfile().ContinueWith(task => Activity.RunOnUiThread(() => view.SetAdapter(new GroupedAdapter(Activity, task.Result.Info.Select(group =>
					new GroupedAdapter.Item(group.Key.ToString(), group.Value.Select(item => new GroupedAdapter.SubItem(item.Key, item.Value))))))));
				return view;
			}
		}

		[MvxUnconventional]
		private class KinksFragment : MvxFragment<CharacterViewModel> {
			public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
				base.OnCreateView(inflater, container, savedInstanceState);

				var view = new RecyclerView(Activity);
				view.SetLayoutManager(new LinearLayoutManager(Activity, LinearLayoutManager.Vertical, false));
				ViewModel.GetProfile().ContinueWith(task => Activity.RunOnUiThread(() => {
					var items = task.Result.Kinks.Select(group => new GroupedAdapter.Item(group.Key.ToString(), group.Value.Select(item => {
						var customKink = item as ProfileViewModel.CustomKink;
						return new GroupedAdapter.SubItem(item.Name, item.Description, customKink?.Children.Select(x => new GroupedAdapter.SubItem(x.Name, x.Description)));
					})));
					view.SetAdapter(new GroupedAdapter(Activity, items));
				}));
				return view;
			}
		}

		[MvxUnconventional]
		private class FriendsFragment : MvxFragment<CharacterViewModel> {
			public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
				base.OnCreateView(inflater, container, savedInstanceState);
				return this.BindingInflate(Resource.Layout.fragment_friends, container, false);
			}
		}
	}
}