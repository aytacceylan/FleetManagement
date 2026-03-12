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
            var drivers = await _db.Drivers.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .ToListAsync();

            var total = drivers.Count;
            var available = drivers.Count(x => x.DriverSituation == "Müsait");
            var driving = drivers.Count(x => x.DriverSituation == "Sürüş Görevi");
            var leave = drivers.Count(x => x.DriverSituation == "İzin");
            var assignment = drivers.Count(x => x.DriverSituation == "Görevlendirme");
            var ydgg = drivers.Count(x => x.DriverSituation == "YDGG");
            var rest = drivers.Count(x => x.DriverSituation == "İstirahat");
            var internalDuty = drivers.Count(x => x.DriverSituation == "Birlik İçi Görev");
            var other = drivers.Count(x => x.DriverSituation == "Diğer");

            DriverSummaryText.Text =
                $"Toplam: {total} | Müsait: {available} | Sürüş: {driving} | İzin: {leave} | Görevl.: {assignment} | YDGG: {ydgg} | İstirahat: {rest} | Birlik İçi: {internalDuty} | Diğer: {other}";
        }

        private async Task LoadVehicleSummaryAsync()
        {
            var vehicles = await _db.Vehicles.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .ToListAsync();

            var total = vehicles.Count;
            var available = vehicles.Count(x => x.VehicleSituation == "Müsait");
            var onDuty = vehicles.Count(x => x.VehicleSituation == "Görevde");
            var kademe = vehicles.Count(x => x.VehicleSituation == "Kademe");
            var servis = vehicles.Count(x => x.VehicleSituation == "Servis");
            var fabrika = vehicles.Count(x => x.VehicleSituation == "Fabrika");

            VehicleSummaryText.Text =
                $"Toplam: {total} | Müsait: {available} | Görevde: {onDuty} | Kademe: {kademe} | Servis: {servis} | Fabrika: {fabrika}";
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
            var rows = await _db.Vehicles.AsNoTracking()
                .Where(x => !x.IsDeleted && x.VehicleSituation == "Müsait")
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
            var rows = await _db.Drivers.AsNoTracking()
                .Where(x => !x.IsDeleted && x.DriverSituation == "Müsait")
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
				var pdfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "FORM 110.pdf");

				if (!File.Exists(pdfPath))
				{
					MessageBox.Show("Assets klasöründe FORM 110.pdf bulunamadı.", "Hata",
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