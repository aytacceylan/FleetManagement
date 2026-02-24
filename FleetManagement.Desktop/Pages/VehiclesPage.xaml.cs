using FleetManagement.Desktop.Data;
using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure; // DbContext namespace'in neyse
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
    public partial class VehiclesPage : Page
    {
        private int? _selectedId;

        public VehiclesPage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async Task LoadAsync(string? q = null)
        {
            using var db = Db.Create<AppDbContext>();

            var query = db.Vehicles.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(x =>
                    x.Plate.Contains(q) ||
                    x.Brand.Contains(q) ||
                    x.Model.Contains(q));
            }

            var list = await query
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            GridVehicles.ItemsSource = list;
            FormInfo.Text = $"Kayıt: {list.Count}";
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _selectedId = null;
            PlateBox.Text = "";
            BrandBox.Text = "";
            ModelBox.Text = "";
            GridVehicles.UnselectAll();
            FormInfo.Text = "Yeni kayıt";
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var plate = PlateBox.Text?.Trim();
            var brand = BrandBox.Text?.Trim();
            var model = ModelBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(plate))
            {
                MessageBox.Show("Plaka zorunlu.");
                return;
            }

            using var db = Db.Create<AppDbContext>();

            if (_selectedId is null)
            {
                db.Vehicles.Add(new Vehicle
                {
                    Plate = plate!,
                    Brand = brand ?? "",
                    Model = model ?? "",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                var entity = await db.Vehicles.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                if (entity is null) { MessageBox.Show("Kayıt bulunamadı."); return; }

                entity.Plate = plate!;
                entity.Brand = brand ?? "";
                entity.Model = model ?? "";
            }

            await db.SaveChangesAsync();
            await LoadAsync(SearchBox.Text);
            FormInfo.Text = "Kaydedildi.";
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedId is null) { MessageBox.Show("Silmek için kayıt seç."); return; }

            if (MessageBox.Show("Seçili aracı silmek istiyor musun?", "Onay",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            using var db = Db.Create<AppDbContext>();
            var entity = await db.Vehicles.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
            if (entity is null) return;

            db.Vehicles.Remove(entity);
            await db.SaveChangesAsync();

            New_Click(sender, e);
            await LoadAsync(SearchBox.Text);
        }

        private void GridVehicles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridVehicles.SelectedItem is not Vehicle v) return;

            _selectedId = v.Id;
            PlateBox.Text = v.Plate;
            BrandBox.Text = v.Brand;
            ModelBox.Text = v.Model;
            FormInfo.Text = $"Seçili Id: {_selectedId}";
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => await LoadAsync(SearchBox.Text);
    }
}