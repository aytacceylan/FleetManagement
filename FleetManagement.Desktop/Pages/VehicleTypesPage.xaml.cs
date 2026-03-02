using FleetManagement.Domain.Entities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FleetManagement.Desktop.Pages
{
	public partial class VehicleTypesPage : Page
	{
		private readonly ObservableCollection<VehicleType> _items = new();
		private ICollectionView? _view;
		private VehicleType? _selected;

		public VehicleTypesPage()
		{
			InitializeComponent();

			_view = CollectionViewSource.GetDefaultView(_items);
			_view.Filter = Filter;

			Grid.ItemsSource = _view;
			UpdateCount();
		}

		private bool Filter(object obj)
		{
			if (obj is not VehicleType x) return false;

			var q = (SearchBox.Text ?? "").Trim().ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(q)) return true;

			return (x.Code ?? "").ToLowerInvariant().Contains(q)
				|| (x.Name ?? "").ToLowerInvariant().Contains(q)
				|| (x.Description ?? "").ToLowerInvariant().Contains(q);
		}

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			_view?.Refresh();
			UpdateCount();
			Notify("Liste yenilendi.");
		}

		private void New_Click(object sender, RoutedEventArgs e)
		{
			Clear_Click(sender, e);
			Notify("Yeni kayıt için form hazır.");
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			var code = (CodeBox.Text ?? "").Trim();
			var name = (NameBox.Text ?? "").Trim();
			var desc = (DescBox.Text ?? "").Trim();

			if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
			{
				Notify("Kod ve Ad zorunludur.", "Uyarı");
				return;
			}

			var exists = _items.Any(x => x != _selected && string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
			if (exists)
			{
				Notify("Bu kod zaten var.", "Uyarı");
				return;
			}

			if (_selected is null)
			{
				var item = new VehicleType
				{
					Id = _items.Count == 0 ? 1 : _items.Max(x => x.Id) + 1,
					Code = code,
					Name = name,
					Description = string.IsNullOrWhiteSpace(desc) ? null : desc,
					CreatedAt = DateTime.Now
				};
				_items.Insert(0, item);
				Notify("Kayıt eklendi.");
			}
			else
			{
				_selected.Code = code;
				_selected.Name = name;
				_selected.Description = string.IsNullOrWhiteSpace(desc) ? null : desc;
				_view?.Refresh();
				Notify("Kayıt güncellendi.");
			}

			UpdateCount();
		}

		private void Delete_Click(object sender, RoutedEventArgs e)
		{
			if (_selected is null)
			{
				Notify("Silmek için kayıt seç.", "Uyarı");
				return;
			}

			_items.Remove(_selected);
			Clear_Click(sender, e);
			UpdateCount();

			Notify("Kayıt silindi.");
		}

		private void Clear_Click(object sender, RoutedEventArgs e)
		{
			_selected = null;
			Grid.SelectedItem = null;

			CodeBox.Text = "";
			NameBox.Text = "";
			DescBox.Text = "";

			Notify("Form temizlendi.");
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			_view?.Refresh();
			UpdateCount();
		}

		private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_selected = Grid.SelectedItem as VehicleType;
			if (_selected is null) return;

			CodeBox.Text = _selected.Code;
			NameBox.Text = _selected.Name;
			DescBox.Text = _selected.Description ?? "";
		}

		private void UpdateCount()
		{
			var count = _view?.Cast<object>().Count() ?? 0;
			FilterInfo.Text = $"Toplam kayıt: {count}";
		}

		private static void Notify(string message, string title = "Bilgi")
		{
			MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}
}