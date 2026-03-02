using FleetManagement.Domain.Entities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace FleetManagement.Desktop.Pages
{
    public partial class VehicleYearsPage : Page
    {
        private readonly ObservableCollection<VehicleYear> _items = new();
        private ICollectionView? _view;
        private VehicleYear? _selected;

        public VehicleYearsPage()
        {
            InitializeComponent();
            _view = CollectionViewSource.GetDefaultView(_items);
            _view.Filter = Filter;
            Grid.ItemsSource = _view;
            UpdateCount();
        }

        private bool Filter(object obj)
        {
            if (obj is not VehicleYear x) return false;
            var q = (SearchBox.Text ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(q)) return true;

            return x.Year.ToString().Contains(q)
                || (x.Note ?? "").ToLowerInvariant().Contains(q);
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
            var yearText = (YearBox.Text ?? "").Trim();
            var note = (NoteBox.Text ?? "").Trim();

            if (!int.TryParse(yearText, out var year) || year < 1900 || year > 2100)
            {
                FormInfo.Text = "Yıl geçersiz. Örnek: 2024";
                return;
            }

            var exists = _items.Any(x => x != _selected && x.Year == year);
            if (exists)
            {
                FormInfo.Text = "Bu yıl zaten var.";
                return;
            }

            if (_selected is null)
            {
                var item = new VehicleYear
                {
                    Id = _items.Count == 0 ? 1 : _items.Max(x => x.Id) + 1,
                    Year = year,
                    Note = string.IsNullOrWhiteSpace(note) ? null : note,
                    CreatedAt = DateTime.Now
                };
                _items.Insert(0, item);
                FormInfo.Text = "Kaydedildi.";
            }
            else
            {
                _selected.Year = year;
                _selected.Note = string.IsNullOrWhiteSpace(note) ? null : note;
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
            YearBox.Text = "";
            NoteBox.Text = "";
            FormInfo.Text = "Temizlendi.";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _view?.Refresh();
            UpdateCount();
        }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selected = Grid.SelectedItem as VehicleYear;
            if (_selected is null) return;

            YearBox.Text = _selected.Year.ToString();
            NoteBox.Text = _selected.Note ?? "";
            FormInfo.Text = $"Seçildi: {_selected.Year}";
        }

        private void UpdateCount()
        {
            var count = _view?.Cast<object>().Count() ?? 0;
            FilterInfo.Text = $"Toplam kayıt: {count}";
        }
    }
}