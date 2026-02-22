using System;
using System.Threading.Tasks;
using System.Windows;
using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;


namespace FleetManagement.Desktop
{
	public partial class MainWindow : Window
	{
		private readonly DbContextOptions<AppDbContext> _dbOptions;

		public MainWindow()
		{
			InitializeComponent();

			_dbOptions = new DbContextOptionsBuilder<AppDbContext>()
				.UseNpgsql("Host=localhost;Port=5432;Database=FleetDb;Username=postgres;Password=1234")
				.Options;

			Loaded += MainWindow_Loaded;
		}

		private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			await LoadVehiclesAsync();
		}

		private async Task LoadVehiclesAsync()
		{
			using var db = new AppDbContext(_dbOptions);

			var vehicles = await db.Vehicles
				.AsNoTracking()
				.OrderByDescending(v => v.Id)
				.ToListAsync();

			VehiclesGrid.ItemsSource = vehicles;
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			using var db = new AppDbContext(_dbOptions);

			var vehicle = new Vehicle
			{
				Plate = "34FAST001",
				Brand = "Toyota",
				Model = "Corolla",
				CreatedAt = DateTime.UtcNow
			};

			db.Vehicles.Add(vehicle);
			await db.SaveChangesAsync();

			MessageBox.Show("Kayıt eklendi. ID: " + vehicle.Id);

			await LoadVehiclesAsync(); // ✅ ekledikten sonra listeyi yenile
		}
		private async void Save_Click(object sender, RoutedEventArgs e)
		{
			var plate = PlateBox.Text?.Trim();
			var brand = BrandBox.Text?.Trim();
			var model = ModelBox.Text?.Trim();

			if (string.IsNullOrWhiteSpace(plate) ||
				string.IsNullOrWhiteSpace(brand) ||
				string.IsNullOrWhiteSpace(model))
			{
				MessageBox.Show("Plaka / Marka / Model boş olamaz.");
				return;
			}

			using var db = new AppDbContext(_dbOptions);

			if (_selectedVehicleId is null)
			{
				// INSERT
				var vehicle = new Vehicle
				{
					Plate = plate,
					Brand = brand,
					Model = model,
					CreatedAt = DateTime.UtcNow
				};

				db.Vehicles.Add(vehicle);
				try
				{
					await db.SaveChangesAsync();
				}
				catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
				{
					MessageBox.Show("Bu plaka zaten kayıtlı. Lütfen farklı bir plaka gir.");
					return;
				}
			}
			else
			{
				// UPDATE
				var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == _selectedVehicleId.Value);
				if (vehicle == null)
				{
					MessageBox.Show("Güncellenecek kayıt bulunamadı (silinmiş olabilir).");
					await LoadVehiclesAsync();
					New_Click(sender, e);
					return;
				}

				vehicle.Plate = plate;
				vehicle.Brand = brand;
				vehicle.Model = model;

				try
				{
					await db.SaveChangesAsync();
				}
				catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
				{
					MessageBox.Show("Bu plaka zaten kayıtlı. Lütfen farklı bir plaka gir.");
					return;
				}
			}

			await LoadVehiclesAsync();
			New_Click(sender, e); // kaydettikten sonra formu temizle
		}
		private async void Delete_Click(object sender, RoutedEventArgs e)
		{
			if (VehiclesGrid.SelectedItem is not Vehicle selected)
			{
				MessageBox.Show("Lütfen listeden silinecek aracı seç.");
				return;
			}

			var confirm = MessageBox.Show(
				$"Silinsin mi?\nPlaka: {selected.Plate}",
				"Silme Onayı",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (confirm != MessageBoxResult.Yes)
				return;

			using var db = new AppDbContext(_dbOptions);

			// Id ile sil (tracking gerektirmez)
			db.Vehicles.Remove(new Vehicle { Id = selected.Id });

			await db.SaveChangesAsync();
			await LoadVehiclesAsync();
		}

		private int? _selectedVehicleId = null;
		private void VehiclesGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (VehiclesGrid.SelectedItem is not Vehicle selected)
				return;

			_selectedVehicleId = selected.Id;

			PlateBox.Text = selected.Plate;
			BrandBox.Text = selected.Brand;
			ModelBox.Text = selected.Model;
		}
		private void New_Click(object sender, RoutedEventArgs e)
		{
			_selectedVehicleId = null;
			VehiclesGrid.SelectedItem = null;

			PlateBox.Clear();
			BrandBox.Clear();
			ModelBox.Clear();

			PlateBox.Focus();
		}
	}
}