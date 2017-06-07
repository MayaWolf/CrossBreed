using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using CrossBreed.Entities;
using CrossBreed.ViewModels;

namespace CrossBreed.Windows.Views {
	public partial class ChangeStatusPopup {
		public ChangeStatusPopup() {
			InitializeComponent();
		}

		private void StatusTextChanged(object sender, TextChangedEventArgs e) {
			TextLength.Text = $"{Encoding.UTF8.GetByteCount(SetStatusText.Text)}/255";
		}

		private void SetStatusButtonClicked(object sender, RoutedEventArgs e) {
			((MainViewModel) DataContext).ChangeStatusCommand.Execute(new Tuple<StatusEnum, string>((StatusEnum) SetStatusComboBox.SelectedItem, SetStatusText.Text));
			IsOpen = false;
		}
	}
}