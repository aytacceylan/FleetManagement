using FleetManagement.Domain.Entities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace FleetManagement.Desktop.Pages
{
    public partial class VehicleBrandsPage : Page
    {
        private readonly ObservableCollection<VehicleBrand> _items = new();
        private ICollectionView? _view;
        private VehicleBrand? _selected;

        public VehicleBrandsPage()
        {
            InitializeComponent();
            _view = CollectionViewSource.GetDefaultView(_items);
            _view.Filter = Filter;
            Grid.ItemsSource = _view;
            UpdateCount();
        }

        private bool Filter(object obj)
        {
            if (obj is not VehicleBrand x) return false;
            var q = (SearchBox.Text ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(q)) return true;

            return (x.Code ?? "").ToLowerInvariant().Contains(q)
                || (x.Name ?? "").ToLowerInvariant().Contains(q)
                || (x.Description ?? "").ToLowerInvariant().Contains(q);
        }

        private void Refresh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _view?.Refresh();
            FormInfo.Text = "Yenilendi.";
            UpdateCount();
        }

        private void New_Click(object sender, System.Windows.RoutedEventArgs e) => Clear_Click(sender, e);

        private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var code = (CodeBox.Text ?? "").Trim();
            var name = (NameBox.Text ?? "").Trim();
            var desc = (DescBox.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(code)) { FormInfo.Text = "Kod boş olamaz."; return; }
            if (string.IsNullOrWhiteSpace(name)) { FormInfo.Text = "Ad boş olamaz."; return; }

            var exists = _items.Any(x => x != _selected && string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
            if (exists) { FormInfo.Text = "Bu kod zaten var."; return; }

            if (_selected is null)
            {
                var item = new VehicleBrand
                {
                    Id = _items.Count == 0 ? 1 : _items.Max(x => x.Id) + 1,
                    Code = code,
                    Name = name,
                    Description = string.IsNullOrWhiteSpace(desc) ? null : desc,
                    CreatedAt = DateTime.Now
                };
                _items.Insert(0, item);
                FormInfo.Text = "Kaydedildi.";
            }
            else
            {
                _selected.Code = code;
                _selected.Name = name;
                _selected.Description = string.IsNullOrWhiteSpace(desc) ? null : desc;
                _view?.Refresh();
                FormInfo.Text = "Güncellendi.";
            }

            UpdateCount();
        }

        private void Delete_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_selected is null) { FormInfo.Text = "Silmek için kayıt seç."; return; }
            _items.Remove(_selected);
            Clear_Click(sender, e);
            FormInfo.Text = "Silindi.";
            UpdateCount();
        }

        private void Clear_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _selected = null;
            Grid.SelectedItem = null;
            CodeBox.Text = "";
            NameBox.Text = "";
            DescBox.Text = "";
            FormInfo.Text = "Temizlendi.";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _view?.Refresh();
            UpdateCount();
        }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selected = Grid.SelectedItem as VehicleBrand;
            if (_selected is null) return;

            CodeBox.Text = _selected.Code;
            NameBox.Text = _selected.Name;
            DescBox.Text = _selected.Description ?? "";
            FormInfo.Text = $"Seçildi: {_selected.Name}";
        }

        private void UpdateCount()
        {
            var count = _view?.Cast<object>().Count() ?? 0;
            FilterInfo.Text = $"Toplam kayıt: {count}";
        }
    }
}