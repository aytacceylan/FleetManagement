using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Views
{
	public partial class VehicleMovementView : UserControl
	{
		private readonly DbContextOptions<AppDbContext> _dbOptions =
			new DbContextOptionsBuilder<AppDbContext>()
				.UseNpgsql("Host=127.0.0.1;Port=5432;Database=FleetDb;Username=postgres;Password=1234")
				.Options;

		public VehicleMovementView()
		{
			InitializeComponent();
			Loaded += VehicleMovementView_Loaded;
		}

		private async void VehicleMovementView_Loaded(object sender, RoutedEventArgs e)
		{
			await LoadLookupsAsync();
			await LoadMovementsAsync();
		}

		private async Task LoadLookupsAsync()
		{
			using var db = new AppDbContext(_dbOptions);

			VehicleCombo.ItemsSource = await db.Vehicles.AsNoTracking()
				.OrderByDescending(x => x.Id).ToListAsync();

            DriverCombo.ItemsSource = await db.Set<Driver>().AsNoTracking()
				.OrderBy(x => x.FullName).ToListAsync();

			CommanderCombo.ItemsSource = await db.Set<VehicleCommander>().AsNoTracking()
				.OrderBy(x => x.FullName).ToListAsync();

			ExitDate.SelectedDate = DateTime.Today;
		}

		private async Task LoadMovementsAsync()
		{
			using var db = new AppDbContext(_dbOptions);

			var rows = await db.Set<VehicleMovement>()
				.AsNoTracking()
				.OrderByDescending(x => x.Id)
				.Select(x => new VehicleMovementRow
				{
					Id = x.Id,
					VehiclePlate = x.Vehicle.Plate,
					DriverName = x.Driver.FullName,
					CommanderName = x.VehicleCommander.FullName,
					ExitDateTime = x.ExitDateTime,
					ReturnDateTime = x.ReturnDateTime,
					Route = x.Route,
					Purpose = x.Purpose,
					KmInfo = x.EndKm == null ? $"{x.StartKm} → ?" : $"{x.StartKm} → {x.EndKm}"
				})
				.ToListAsync();

			MovementsGrid.ItemsSource = rows;
		}

		private async void RefreshMovements_Click(object sender, RoutedEventArgs e)
		{
			await LoadMovementsAsync();
		}

		private async void SaveMovement_Click(object sender, RoutedEventArgs e)
		{
			if (VehicleCombo.SelectedValue is not int vehicleId)
			{
				MessageBox.Show("Araç seçmelisin.");
				return;
			}
			if (DriverCombo.SelectedValue is not int driverId)
			{
				MessageBox.Show("Sürücü seçmelisin.");
				return;
			}
			if (CommanderCombo.SelectedValue is not int commanderId)
			{
				MessageBox.Show("Araç komutanı seçmelisin.");
				return;
			}

			if (string.IsNullOrWhiteSpace(RouteBox.Text) || string.IsNullOrWhiteSpace(PurposeBox.Text))
			{
				MessageBox.Show("Güzergah ve amaç zorunlu.");
				return;
			}

			if (!int.TryParse(StartKmBox.Text, out var startKm))
			{
				MessageBox.Show("Başlangıç KM sayı olmalı.");
				return;
			}

			int? endKm = null;
			if (!string.IsNullOrWhiteSpace(EndKmBox.Text))
			{
				if (int.TryParse(EndKmBox.Text, out var parsed))
					endKm = parsed;
				else
				{
					MessageBox.Show("Bitiş KM sayı olmalı.");
					return;
				}
			}

			var exitDate = ExitDate.SelectedDate ?? DateTime.Today;
			var returnDate = ReturnDate.SelectedDate;

			using var db = new AppDbContext(_dbOptions);

			var movement = new VehicleMovement
			{
				VehicleId = vehicleId,
				DriverId = driverId,
				VehicleCommanderId = commanderId,
				ExitDateTime = DateTime.SpecifyKind(exitDate, DateTimeKind.Utc),
				ReturnDateTime = returnDate == null ? null : DateTime.SpecifyKind(returnDate.Value, DateTimeKind.Utc),
				Route = RouteBox.Text.Trim(),
				Purpose = PurposeBox.Text.Trim(),
				Description = string.IsNullOrWhiteSpace(DescBox.Text) ? null : DescBox.Text.Trim(),
				StartKm = startKm,
				EndKm = endKm,
				CreatedAt = DateTime.UtcNow,
				IsDeleted = false
			};

			db.Set<VehicleMovement>().Add(movement);
			await db.SaveChangesAsync();

			MessageBox.Show("Hareket kaydedildi. ID: " + movement.Id);

			RouteBox.Text = "";
			PurposeBox.Text = "";
			DescBox.Text = "";
			StartKmBox.Text = "";
			EndKmBox.Text = "";
			ReturnDate.SelectedDate = null;

			await LoadMovementsAsync();
		}
	}
}