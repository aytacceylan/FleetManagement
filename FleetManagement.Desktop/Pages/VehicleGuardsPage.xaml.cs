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
    public partial class VehicleGuardsPage : Page
    {
        private readonly ObservableCollection<VehicleGuard> _items = new();
        private ICollectionView? _view;
        private VehicleGuard? _selected;

        public VehicleGuardsPage()
        {
            InitializeComponent();

            // Sunum/başlangıç: boş liste ile çalışır (istersen demo data ekleyebilirsin)
            _view = CollectionViewSource.GetDefaultView(_items);
            _view.Filter = Filter;

            GuardsGrid.ItemsSource = _view;
            UpdateCount();
        }

        private bool Filter(object obj)
        {
            if (obj is not VehicleGuard x) return false;
            var q = (SearchBox.Text ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(q)) return true;

            return (x.GuardNumber ?? "").ToLowerInvariant().Contains(q)
                || (x.FullName ?? "").ToLowerInvariant().Contains(q)
                || (x.PhoneNumber ?? "").ToLowerInvariant().Contains(q);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            _view?.Refresh();
            FormInfo.Text = "Yenilendi.";
            UpdateCount();
        }

        private void New_Click(object sender, RoutedEventArgs e) => Clear_Click(sender, e);

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var no = (GuardNumberBox.Text ?? "").Trim();
            var name = (FullNameBox.Text ?? "").Trim();
            var phone = (PhoneBox.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(no))
            {
                FormInfo.Text = "Muhafız No boş olamaz.";
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                FormInfo.Text = "Ad Soyad boş olamaz.";
                return;
            }

            // Unique kontrol (RAM)
            var exists = _items.Any(x => x != _selected && string.Equals(x.GuardNumber, no, StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                FormInfo.Text = "Bu Muhafız No zaten var.";
                return;
            }

            if (_selected is null)
            {
                var item = new VehicleGuard
                {
                    Id = _items.Count == 0 ? 1 : _items.Max(x => x.Id) + 1,
                    GuardNumber = no,
                    FullName = name,
                    PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone,
                    CreatedAt = DateTime.Now
                };
                _items.Insert(0, item);
                FormInfo.Text = "Kaydedildi.";
            }
            else
            {
                _selected.GuardNumber = no;
                _selected.FullName = name;
                _selected.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone;
                FormInfo.Text = "Güncellendi.";
                _view?.Refresh();
            }

            UpdateCount();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selected is null)
            {
                FormInfo.Text = "Silmek için listeden kayıt seç.";
                return;
            }

            _items.Remove(_selected);
            Clear_Click(sender, e);
            FormInfo.Text = "Silindi.";
            UpdateCount();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _selected = null;
            GuardsGrid.SelectedItem = null;

            GuardNumberBox.Text = "";
            FullNameBox.Text = "";
            PhoneBox.Text = "";

            FormInfo.Text = "Temizlendi.";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _view?.Refresh();
            UpdateCount();
        }

        private void GuardsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selected = GuardsGrid.SelectedItem as VehicleGuard;
            if (_selected is null) return;

            GuardNumberBox.Text = _selected.GuardNumber;
            FullNameBox.Text = _selected.FullName;
            PhoneBox.Text = _selected.PhoneNumber ?? "";
            FormInfo.Text = $"Seçildi: {_selected.FullName}";
        }

        private void UpdateCount()
        {
            var count = _view?.Cast<object>().Count() ?? 0;
            FilterInfo.Text = $"Toplam kayıt: {count}";
        }
    }
}