using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;


namespace FleetManagement.Desktop.Pages
{
	public partial class HomePage : Page
	{
		private readonly AppDbContext _db = new(App.DbOptions);

		private sealed class ActiveVehicleRow
		{
			public string Plate { get; set; } = "";
			public string? Driver { get; set; }
			public string ExitTime { get; set; } = "";
			public string? Route { get; set; }
			public string? VehicleBrand { get; set; }
		}

		private sealed class MaintenanceRow
		{
			public string Plate { get; set; } = "";
			public string? VehicleBrand { get; set; }
			public int? CurrentKm { get; set; }
			public int? LastMaintenanceKm { get; set; }
			public string? LastMaintenanceDateText { get; set; }
			public string Status { get; set; } = "";
		}


		private sealed class AvailableVehicleRow
		{
			public string Plate { get; set; } = "";
			public string? VehicleBrand { get; set; }
			public string? VehicleUnit { get; set; }
			public int? VehicleKm { get; set; }
		}

		private sealed class AvailableDriverRow
		{
			public string? FullName { get; set; }
			public string? DriverNumber { get; set; }
			public string? PhoneNumber { get; set; }
		}

		private sealed class OngoingMovementRow
		{
			public string Plate { get; set; } = "";
			public string? Driver { get; set; }
			public string ExitTime { get; set; } = "";
			public string? Route { get; set; }
			public string? VehicleBrand { get; set; }
		}

		public HomePage()
		{
			InitializeComponent();

			Loaded += async (_, __) => await LoadDashboardAsync();
			var timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromSeconds(30);
			timer.Tick += async (_, __) => await LoadDashboardAsync();
			timer.Start();


		}

		private async Task LoadDashboardAsync()
		{
			await LoadDriverSummaryAsync();
			await LoadVehicleSummaryAsync();
			await LoadMaintenanceSummaryAsync();
			await LoadTodaySummaryAsync();

			await LoadAvailableVehiclesAsync();
			await LoadAvailableDriversAsync();
			await LoadMaintenanceGridAsync();
			await LoadOngoingMovementsAsync();

			LastUpdateText.Text = $"Son Güncelleme: {DateTime.Now:HH:mm}";
		}

		private async Task LoadDriverSummaryAsync()
		{
			var totalDrivers = await _db.Drivers.AsNoTracking()
				.CountAsync(x => !x.IsDeleted);

			var onDutyDriverIds = await _db.VehicleMovements.AsNoTracking()
				.Where(x => !x.IsDeleted && x.ReturnDateTime == null && x.DriverId != null)
				.Select(x => x.DriverId!.Value)
				.Distinct()
				.ToListAsync();

			var onDutyCount = onDutyDriverIds.Count;
			var availableCount = totalDrivers - onDutyCount;

			DriverSummaryText.Text = $"Toplam: {totalDrivers} | Görevde: {onDutyCount} | Müsait: {availableCount}";
		}

		private async Task LoadVehicleSummaryAsync()
		{
			var totalVehicles = await _db.Vehicles.AsNoTracking()
				.CountAsync(x => !x.IsDeleted);

			var movements = await _db.VehicleMovements.AsNoTracking()
				.Where(x => !x.IsDeleted && x.VehicleId != null && x.ReturnDateTime == null)
				.Include(x => x.Vehicle)
				.ToListAsync();

			var now = DateTime.UtcNow;

			var ongoing = movements.Count(x => x.ExitDateTime <= now);
			var planned = movements.Count(x => x.ExitDateTime > now);

			var busyVehicleIds = movements
				.Where(x => x.VehicleId != null)
				.Select(x => x.VehicleId!.Value)
				.Distinct()
				.Count();

			var available = totalVehicles - busyVehicleIds;

			VehicleSummaryText.Text = $"Toplam: {totalVehicles} | Devam: {ongoing} | Planlı: {planned} | Müsait: {available}";
		}

		private async Task LoadMaintenanceSummaryAsync()
		{
			var vehicles = await _db.Vehicles.AsNoTracking()
				.Where(x => !x.IsDeleted)
				.ToListAsync();

			var delayed = 0;
			var soon = 0;
			var normal = 0;

			foreach (var v in vehicles)
			{
				var status = CalcMaintenanceStatus(
					v.VehicleKm,
					v.MaintenanceIntervalKm,
					v.MaintenanceIntervalMonths,
					v.LastMaintenanceKm,
					v.LastMaintenanceDate);

				if (status == "Gecikti") delayed++;
				else if (status == "Yaklaşıyor") soon++;
				else if (status == "Normal") normal++;
			}

			MaintenanceSummaryText.Text = $"Geciken: {delayed} | Yaklaşan: {soon} | Normal: {normal}";
		}

		private async Task LoadTodaySummaryAsync()
		{
			var today = DateTime.Today;

			var todayMovements = await _db.VehicleMovements.AsNoTracking()
				.Where(x => !x.IsDeleted)
				.ToListAsync();

			todayMovements = todayMovements
				.Where(x => x.ExitDateTime.ToLocalTime().Date == today)
				.ToList();

			var completed = todayMovements.Count(x => x.ReturnDateTime != null);
			var planned = todayMovements.Count(x => x.ReturnDateTime == null && x.ExitDateTime.ToLocalTime() > DateTime.Now);
			var ongoing = todayMovements.Count(x => x.ReturnDateTime == null && x.ExitDateTime.ToLocalTime() <= DateTime.Now);

			TodaySummaryText.Text = $"Toplam: {todayMovements.Count} | Tamamlanan: {completed} | Devam: {ongoing} | Planlı: {planned}";
		}

		private async Task LoadMaintenanceGridAsync()
		{
			var vehicles = await _db.Vehicles.AsNoTracking()
				.Where(x => !x.IsDeleted)
				.OrderBy(x => x.Plate)
				.ToListAsync();

			var rows = new List<MaintenanceRow>();

			foreach (var v in vehicles)
			{
				var status = CalcMaintenanceStatus(
					v.VehicleKm,
					v.MaintenanceIntervalKm,
					v.MaintenanceIntervalMonths,
					v.LastMaintenanceKm,
					v.LastMaintenanceDate);

				if (status == "Gecikti" || status == "Yaklaşıyor")
				{
					rows.Add(new MaintenanceRow
					{
						Plate = v.Plate,
						VehicleBrand = v.VehicleBrand,
						CurrentKm = v.VehicleKm,
						LastMaintenanceKm = v.LastMaintenanceKm,
						LastMaintenanceDateText = v.LastMaintenanceDate?.ToLocalTime().ToString("dd.MM.yyyy"),
						Status = status
					});
				}
			}

			MaintenanceGrid.ItemsSource = rows;
		}

		private static string CalcMaintenanceStatus(
			int? currentKm,
			int? intervalKm,
			int? intervalMonths,
			int? lastKm,
			DateTime? lastDate)
		{
			if (intervalKm is null && intervalMonths is null)
				return "Tanımsız";

			if (lastKm is null && lastDate is null)
				return "Bakım Gir";

			bool overdue = false;
			bool soon = false;

			if (intervalKm is not null && currentKm is not null && lastKm is not null)
			{
				var dueKm = lastKm.Value + intervalKm.Value;

				if (currentKm.Value >= dueKm)
					overdue = true;
				else if (currentKm.Value >= dueKm - 1000)
					soon = true;
			}

			if (intervalMonths is not null && lastDate is not null)
			{
				var dueDate = lastDate.Value.Date.AddMonths(intervalMonths.Value);
				var today = DateTime.Today;

				if (today >= dueDate)
					overdue = true;
				else if (today >= dueDate.AddDays(-30))
					soon = true;
			}

			if (overdue) return "Gecikti";
			if (soon) return "Yaklaşıyor";
			return "Normal";
		}


		private async Task LoadAvailableVehiclesAsync()
		{
			var busyVehicleIds = await _db.VehicleMovements.AsNoTracking()
				.Where(x => !x.IsDeleted && x.ReturnDateTime == null && x.VehicleId != null)
				.Select(x => x.VehicleId!.Value)
				.Distinct()
				.ToListAsync();

			var rows = await _db.Vehicles.AsNoTracking()
				.Where(x => !x.IsDeleted && !busyVehicleIds.Contains(x.Id))
				.OrderBy(x => x.Plate)
				.Select(x => new AvailableVehicleRow
				{
					Plate = x.Plate,
					VehicleBrand = x.VehicleBrand,
					VehicleUnit = x.VehicleUnit,
					VehicleKm = x.VehicleKm
				})
				.ToListAsync();

			AvailableVehiclesGrid.ItemsSource = rows;
		}

		private async Task LoadAvailableDriversAsync()
		{
			var busyDriverIds = await _db.VehicleMovements.AsNoTracking()
				.Where(x => !x.IsDeleted && x.ReturnDateTime == null && x.DriverId != null)
				.Select(x => x.DriverId!.Value)
				.Distinct()
				.ToListAsync();

			var rows = await _db.Drivers.AsNoTracking()
				.Where(x => !x.IsDeleted && !busyDriverIds.Contains(x.Id))
				.OrderBy(x => x.FullName)
				.Select(x => new AvailableDriverRow
				{
					FullName = x.FullName,
					DriverNumber = x.DriverNumber,
					PhoneNumber = x.PhoneNumber
				})
				.ToListAsync();

			AvailableDriversGrid.ItemsSource = rows;
		}

		private async Task LoadOngoingMovementsAsync()
		{
			var rows = await _db.VehicleMovements.AsNoTracking()
				.Where(x => !x.IsDeleted && x.ReturnDateTime == null)
				.Include(x => x.Vehicle)
				.Include(x => x.Driver)
				.OrderBy(x => x.ExitDateTime)
				.Select(x => new OngoingMovementRow
				{
					Plate = x.Vehicle != null ? x.Vehicle.Plate : (x.VehiclePlateText ?? ""),
					Driver = x.Driver != null ? x.Driver.FullName : x.DriverText,
					ExitTime = x.ExitDateTime.ToLocalTime().ToString("HH:mm"),
					Route = x.Route,
					VehicleBrand = x.Vehicle != null ? x.Vehicle.VehicleBrand : null
				})
				.ToListAsync();

			OngoingMovementsGrid.ItemsSource = rows;
		}

		private void PreparationForm_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var pdfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "ÇıkışFormu.pdf");

				if (!File.Exists(pdfPath))
				{
					MessageBox.Show("Assets klasöründe ÇıkışFormu.pdf bulunamadı.", "Hata",
						MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				Process.Start(new ProcessStartInfo
				{
					FileName = pdfPath,
					UseShellExecute = true
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Hata",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void VehicleList_Click(object sender, RoutedEventArgs e)
		{
			NavigationService?.Navigate(new VehiclesPage());
		}

		private void DriverList_Click(object sender, RoutedEventArgs e)
		{
			NavigationService?.Navigate(new DriversPage());
		}

		private async void Refresh_Click(object sender, RoutedEventArgs e)
		{
			await LoadDashboardAsync();
		}


	}
}