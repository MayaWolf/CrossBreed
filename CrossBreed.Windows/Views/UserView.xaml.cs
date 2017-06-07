using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using CrossBreed.Entities;
using CrossBreed.ViewModels;
using ML.Collections;
using MvvmCross.Plugins.Color.Wpf;
using ML.DependencyProperty;
using MvvmCross.Platform;

namespace CrossBreed.Windows.Views {
	public partial class UserView {
		private static readonly IDictionary<StatusEnum, ImageSource> statusImages = new Dictionary<StatusEnum, ImageSource>();
		private Run nameRun;

		[DependencyProperty(ChangedCallback = nameof(CharacterPropertyChanged))]
		public CharacterViewModel Character { get; set; }
		
		[DependencyProperty(ChangedCallback = nameof(ChannelMemberChanged))]
		public ChannelMemberViewModel ChannelMember { get; set; }

		public UserView() {
			InitializeComponent();
		}

		protected virtual void CharacterPropertyChanged(DependencyPropertyChangedEventArgs e) {
			Inlines.Clear();
			if(e.OldValue is CharacterViewModel oldValue) oldValue.CharacterLists.CollectionChanged -= CharacterListsChanged;
			if(e.NewValue == null) return;
			var character = (CharacterViewModel)e.NewValue;
			Inlines.Add(new InlineUIContainer(new Image {
				Source = GetStatusImage(character.Character.Status), Height = 15, Width = 15, UseLayoutRounding = true, SnapsToDevicePixels = true
			}) {
				BaselineAlignment = BaselineAlignment.TextBottom
			});
			Inlines.Add(new Run(" "));
			nameRun = new Run(character.Character.Name) { Foreground = new SolidColorBrush(MvxWpfColor.ToNativeColor(Character.GenderColor)) };
			ApplyListColors();
			character.CharacterLists.CollectionChanged += CharacterListsChanged;
			Inlines.Add(nameRun);
		}

		private void CharacterListsChanged(object sender, NotifyCollectionChangedEventArgs args) => ApplyListColors();

		private void ApplyListColors() {
			IEnumerable<CharacterList> lists = Character.CharacterLists.ToList();
			if(ChannelMember != null && ChannelMember.Member.Rank > Channel.RankEnum.User) {
				lists = lists.Concat(Mvx.GetSingleton<CharacterListProvider>().ChannelOps.SingletonEnumerable());
			}
			var list = lists.OrderBy(x => x.SortingOrder).FirstOrDefault();
			if(list != null) {
				var bytes = BitConverter.GetBytes(list.UnderlineColor);
				if(bytes[3] != 0) {
					nameRun.TextDecorations.Add(new TextDecoration(TextDecorationLocation.Underline,
						new Pen(new SolidColorBrush(Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0])), 1.5), 0, 0, TextDecorationUnit.FontRecommended));
				} else nameRun.TextDecorations.Clear();
				bytes = BitConverter.GetBytes(list.TextColor);
				nameRun.Foreground = new SolidColorBrush(bytes[3] != 0 ? Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]) : MvxWpfColor.ToNativeColor(Character.GenderColor));
			}
		}

		private void ChannelMemberChanged(DependencyPropertyChangedEventArgs e) {
			Character = ((ChannelMemberViewModel) e.NewValue).Character;
		}

		private static ImageSource GetStatusImage(StatusEnum status) {
			if(!statusImages.ContainsKey(status)) {
				var group = new DrawingGroup();
				var circle = Geometry.Parse("M16,1a1,1 0 0,0 0,22a1,1 0 0,0 0,-22z");
				switch(status) {
					case StatusEnum.Offline:
						group.Children.Add(new GeometryDrawing(Brushes.DarkGray, new Pen(Brushes.DimGray, 1), circle));
						group.Children.Add(new GeometryDrawing(null, new Pen(Brushes.White, 1.5), Geometry.Parse("M10.5,17.5l11,-11m0,11l-11,-11")));
						break;
					case StatusEnum.Online:
						group.Children.Add(new GeometryDrawing(Brushes.Black, new Pen(Brushes.Gray, 1), circle));
						group.Children.Add(new GeometryDrawing(null, new Pen(Brushes.White, 1.5), Geometry.Parse("M16,5.5a4,5 0 0,0 0,13a4,5 0 0,0 0,-13z")));
						break;
					case StatusEnum.Away:
						group.Children.Add(new GeometryDrawing(Brushes.DarkBlue, new Pen(new SolidColorBrush(Color.FromRgb(0, 0, 0x33)), 1), circle));
						group.Children.Add(new GeometryDrawing(null, new Pen(Brushes.White, 1.5), Geometry.Parse("M11,18l5,-12l5,12m-2,-4h-7")));
						break;
					case StatusEnum.Idle:
						group.Children.Add(new GeometryDrawing(Brushes.Yellow, new Pen(Brushes.Olive, 1), circle));
						group.Children.Add(new GeometryDrawing(null, new Pen(Brushes.Black, 2), Geometry.Parse("M16,5v14")));
						break;
					case StatusEnum.Looking:
						group.Children.Add(new GeometryDrawing(Brushes.LimeGreen, new Pen(Brushes.DarkGreen, 1), circle));
						group.Children.Add(new GeometryDrawing(null, new Pen(Brushes.Black, 2), Geometry.Parse("M14,5v12h6")));
						break;
					case StatusEnum.Busy:
						group.Children.Add(new GeometryDrawing(Brushes.CornflowerBlue, new Pen(Brushes.Blue, 1), circle));
						group.Children.Add(new GeometryDrawing(null, new Pen(Brushes.Black, 1.5), Geometry.Parse("M14,5v13h2a1,1 0 0,0 0,-6.75h-2m2,0a1,1 0 0,0 0,-5.5h-2")));
						break;
					case StatusEnum.DND:
						group.Children.Add(new GeometryDrawing(Brushes.Red, new Pen(Brushes.DarkRed, 1), circle));
						group.Children.Add(new GeometryDrawing(null, new Pen(Brushes.White, 1.5), Geometry.Parse("M12.5,5v13h3a1,1 0 0,0 0,-12.25h-3")));
						break;
				}
				statusImages.Add(status, new DrawingImage(group));
			}
			return statusImages[status];
		}

		private void ProfileButtonClicked(object sender, RoutedEventArgs e) {
			if(UserSettings.Instance.General.UseProfileViewer) Character.ShowProfileCommand.Execute();
			else System.Diagnostics.Process.Start("https://www.f-list.net/c/" + Character.Character.Name);
		}

		private void UserView_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			Character.MessageCommand.Execute();
		}

		private void AddListItemClicked(object sender, MouseButtonEventArgs e) {
			
		}

		private void RemoveListItemClicked(object sender, MouseButtonEventArgs e) {
			
		}

		private void ManageListsButtonClicked(object sender, RoutedEventArgs e) {
			((Button) sender).ContextMenu.IsOpen = true;
			e.Handled = true;
		}

		private void ContextMenuClick(object sender, RoutedEventArgs routedEventArgs) {
			((ContextMenu) sender).IsOpen = false;
		}
	}
}