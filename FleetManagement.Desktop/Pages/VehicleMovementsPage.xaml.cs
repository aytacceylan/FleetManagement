using ClosedXML.Excel;
using FleetManagement.Application.Helpers;
using FleetManagement.Desktop.Dtos;
using FleetManagement.Desktop.Helpers;
using FleetManagement.Desktop.Services;
using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace FleetManagement.Desktop.Pages
{
	public partial class VehicleMovementsPage : Page
	{
		private readonly AppDbContext _db = new(App.DbOptions);

		private int? _selectedId;

        // Yeni ekle
        private VehicleMovement? _currentMovement;

        private List<VehicleMovementRow> _all = new();

        private List<Driver> _availableDrivers = new();

        public VehicleMovementsPage()
		{
			InitializeComponent();

			SetExitNow();
			ReturnDatePicker.SelectedDate = null;
			ReturnTimeBox.Text = "";
			UpdateReturnHighlight();

			Loaded += async (_, __) =>
			{
				await LoadLookupsAsync();
				await LoadAsync();
				PrepareNewFormState();

				// Aylık rapor kontrolünü arka planda başlat
				_ = CheckMonthlyReportAsync();
			};
		}


		// =========================
		// LOOKUPS
		// =========================
		private async Task LoadLookupsAsync()
		{
            var vehicles = await _db.Vehicles
        .AsNoTracking()
        .Where(x => !x.IsDeleted)
        .OrderBy(x => x.Plate)
        .ToListAsync();

            ComboBoxSearchHelper.BindContains(
                VehicleCombo,
                vehicles,
                nameof(Vehicle.Plate),
                nameof(Vehicle.Id),
                x => x.Plate ?? "");


			// 1. sürücü
			var drivers = await _db.Drivers
				.AsNoTracking()
				.Where(x =>
					!x.IsDeleted &&
					x.DriverSituation == "Müsait")
				.OrderBy(x => x.FullName)
				.ToListAsync();

			_availableDrivers = drivers;



			ComboBoxSearchHelper.BindContains(
				DriverCombo,
				drivers,
				nameof(Driver.FullName),
				nameof(Driver.Id),
				x => x.FullName ?? "");



			var seconddrivers = await _db.Drivers
				.AsNoTracking()
				.Where(x => !x.IsDeleted)
				.OrderBy(x => x.FullName)
				.ToListAsync();

			ComboBoxSearchHelper.BindContains(
				SecondDriverCombo,
				seconddrivers,
				nameof(Driver.FullName),
				nameof(Driver.Id),
				x => x.FullName ?? "");

			var commanders = await _db.VehicleCommanders.AsNoTracking()
	.Where(x => !x.IsDeleted)
	.OrderBy(x => x.FullName)
	.Select(x => new { x.Id, Display = x.FullName })
	.ToListAsync();

			CommanderCombo.ItemsSource = commanders;
			CommanderCombo.DisplayMemberPath = "Display";
			CommanderCombo.SelectedValuePath = "Id";

			var routes = await _db.Routes
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

            ComboBoxSearchHelper.BindContains(
                RouteCombo,
                routes,
                nameof(Route.Name),
                nameof(Route.Name),
                x => x.Name ?? "");

            var departures = await _db.Departures
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .ToListAsync();

            ComboBoxSearchHelper.BindContains(
                DepartureCombo,
                departures,
                nameof(Departure.Name),
                nameof(Departure.Name),
                x => x.Name ?? "");

            var dutyTypes = await _db.DutyTypes
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .ToListAsync();

            ComboBoxSearchHelper.BindContains(
                DutyTypeCombo,
                dutyTypes,
                nameof(DutyType.Name),
                nameof(DutyType.Name),
                x => x.Name ?? "");

            var vehicleTypes = await _db.VehicleTypes
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .ToListAsync();

            ComboBoxSearchHelper.BindContains(
                VehicleTypeCombo,
                vehicleTypes,
                nameof(VehicleType.Name),
                nameof(VehicleType.Name),
                x => x.Name ?? "");
        }

		// =========================
		// GRID LOAD
		// =========================
		private async Task LoadAsync()
		{
            // Bugünün başlangıcı (00:00)
            var todayStart = DateTime.Today;



            // UTC'ye çevir
            var todayStartUtc = DateTime.SpecifyKind(todayStart, DateTimeKind.Local).ToUniversalTime();


            var query = _db.VehicleMovements
                .AsNoTracking()
                .Where(x => !x.IsDeleted);



            var raw = await query
                .Include(x => x.Vehicle)
                .Include(x => x.Driver)
                .Include(x => x.SecondDriver)
                .Include(x => x.VehicleCommander)
                .OrderByDescending(x => x.Id)
                .Take(3000)
                .ToListAsync();

			var rows = raw.Select(m =>
			{
				var exitLocal = m.ExitDateTime.ToLocalTime();
				var returnLocal = m.ReturnDateTime?.ToLocalTime();
				var parsed = ParseLoadOrPassengerInfo(m.LoadOrPassengerInfo);

				var status = CalcStatus(m);

				int? doneKm = null;
				if (m.StartKm.HasValue && m.EndKm.HasValue && m.EndKm.Value >= m.StartKm.Value)
					doneKm = m.EndKm.Value - m.StartKm.Value;

				var dateForNo = m.MovementDate == default
					? m.ExitDateTime.ToLocalTime().Date
					: m.MovementDate.ToLocalTime().Date;

				return new VehicleMovementRow
				{
					Id = m.Id,
					MovementNo = $"{dateForNo:yyyyMMdd}-{m.DailyNo:000}",
					DailyNo = m.DailyNo,
					Driver = m.Driver?.FullName ?? m.DriverText,
					SecondDriver = m.SecondDriver?.FullName ?? m.SecondDriverText,
					Plate = m.Vehicle?.Plate ?? m.VehiclePlateText ?? "",
					ExitTimeText = exitLocal.ToString("HH:mm"),
					ReturnTimeText = returnLocal is null ? "—" : returnLocal.Value.ToString("HH:mm"),
					VehicleBrand = m.Vehicle?.VehicleBrand,
					VehicleType = m.Vehicle?.VehicleType,
					Status = status,
					StatusBrush = GetStatusBrush(status),
					DateText = exitLocal.ToString("dd.MM.yyyy"),
					Route = m.Route,
					Commander = m.VehicleCommander?.FullName ?? m.CommanderText,
					Departure = m.Purpose,
					DoneKm = doneKm,
					PassengerCount = parsed.passenger,
					LoadAmount = parsed.load,
					DutyType = m.Description,
					ExitDateTimeUtc = m.ExitDateTime,
					ReturnDateTimeUtc = m.ReturnDateTime,
					IsPreviousDayOpen =
					m.ReturnDateTime == null &&
					m.ExitDateTime < todayStartUtc,
				};
			}).ToList();

            // Varsayılan görünüm: Güncel görevler
            if (!ShowAllCheckBox.IsChecked.GetValueOrDefault())
            {
                rows = rows.Where(x =>
                    // Planlandı
                    x.Status == "Planlandı"

                    ||

                    // Görev yaklaşıyor
                    x.Status == "Görev Yaklaşıyor"

                    ||

                    // Görev gecikti
                    x.Status == "Görev Gecikti"

                    ||

                    // Devam eden görev
                    x.Status == "Görevde"

                    ||

                    // Bugün tamamlanan görevler
                    (
                        x.Status == "Tamamlandı" &&
                        x.ReturnDateTimeUtc.HasValue &&
                        x.ReturnDateTimeUtc.Value.ToLocalTime().Date == DateTime.Today
                    )
                ).ToList();
            }

			_all = rows;
			MovementsGrid.ItemsSource = _all;

			MovementsGrid.SelectedItem = null;

			UpdateCount(_all.Count);
		}

		private void UpdateCount(int count)
		{
			FilterInfo.Text = $"Toplam kayıt: {count}";
		}

        // =========================
        // UI EVENTS
        // =========================
        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadLookupsAsync();
            await RepairMissionStatesAsync();
            await LoadAsync();

            _selectedId = null;

            ClearForm();

            PrepareNewFormState();

            Notify("Veriler güncellendi.");
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _selectedId = null;

            ClearForm();

            PrepareNewFormState();

            Notify("Yeni kayıt için form hazır.");
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _selectedId = null;

            ClearForm();

            PrepareNewFormState();

            Notify("Form temizlendi.");
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!TryBuildDateTime(ExitDatePicker, ExitTimeBox, out var exitDtLocal, out var err1))
				{
					Notify("Çıkış zamanı hatalı: " + err1, "Uyarı");
					return;
				}

				if (!TryBuildNullableDateTime(ReturnDatePicker, ReturnTimeBox, out var returnDtLocal, out var err2))
				{
					Notify("Dönüş zamanı hatalı: " + err2, "Uyarı");
					return;
				}

				if (returnDtLocal is not null && returnDtLocal < exitDtLocal)
				{
					Notify("Dönüş zamanı çıkıştan önce olamaz.", "Uyarı");
					return;
				}

				var vehicleId = VehicleCombo.SelectedValue is int vid ? vid : (int?)null;
				var plateText = EmptyToNull(VehicleCombo.Text);

				if (vehicleId is null && string.IsNullOrWhiteSpace(plateText))
				{
					Notify("Plaka zorunlu. Listeden araç seçin.", "Uyarı");
					return;
				}

				if (DriverCombo.SelectedValue is not int didValue)
				{
					Notify(
						$"Sürücü seçimi okunamadı.\n" +
						$"Text: {DriverCombo.Text}\n" +
						$"SelectedValue: {DriverCombo.SelectedValue}",
						"Uyarı");

					return;
				}



				Driver? selectedDriver = null;
				Driver? selectedSecondDriver = null;
				Vehicle? selectedVehicle = null;

				if (vehicleId is not null)
				{
					selectedVehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId.Value && !v.IsDeleted);

					if (selectedVehicle is null)
					{
						Notify("Araç bulunamadı.", "Uyarı");
						return;
					}

					if (_selectedId is null && IsVehicleBlockedForDispatch(selectedVehicle.VehicleSituation))
					{
						Notify($"Bu araç sevke uygun değil. Araç durumu: {NormalizeVehicleSituation(selectedVehicle.VehicleSituation)}", "Uyarı");
						return;
					}
				}

				selectedDriver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == didValue && !d.IsDeleted);

				if (selectedDriver is null)
				{
					Notify("Sürücü bulunamadı.", "Uyarı");
					return;
				}
				if (_selectedId is null && IsDriverBlockedForDispatch(selectedDriver.DriverSituation))
				{
					Notify($"Bu sürücü sevke uygun değil. Sürücü durumu: {NormalizeDriverSituation(selectedDriver.DriverSituation)}", "Uyarı");
					return;
				}

				if (SecondDriverCombo.SelectedValue is int secondDidValue)
				{
					if (secondDidValue == didValue)
					{
						Notify("1. sürücü ile 2. sürücü aynı kişi olamaz.", "Uyarı");
						return;
					}

					selectedSecondDriver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == secondDidValue && !d.IsDeleted);

					if (selectedSecondDriver is null)
					{
						Notify("2. sürücü bulunamadı.", "Uyarı");
						return;
					}

					if (_selectedId is null && IsDriverBlockedForDispatch(selectedSecondDriver.DriverSituation))
					{
						Notify($"2. sürücü sevke uygun değil. Sürücü durumu: {NormalizeDriverSituation(selectedSecondDriver.DriverSituation)}", "Uyarı");
						return;
					}
				}

                if (_selectedId is null && vehicleId is not null)
                {
                    var hasOpenMovement = await _db.VehicleMovements
                        .AsNoTracking()
                        .Where(m => !m.IsDeleted &&
                                    m.VehicleId == vehicleId)
                        .ToListAsync();

                    if (hasOpenMovement.Any(MissionHelper.IsActiveMission))
                    {
                        MessageBox.Show(
                            "Bu araç halen aktif bir görevde. Yeni görev tanımlanamaz.",
                            "Uyarı",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }
                }

                var entity = _selectedId is null
					? new VehicleMovement { CreatedAt = DateTime.UtcNow, IsDeleted = false }
					: await _db.VehicleMovements.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);

				if (entity is null)
				{
					Notify("Kayıt bulunamadı.", "Uyarı");
					return;
				}

				// ESKİ değerleri sakla
				var oldVehicleId = entity.VehicleId;
				var oldDriverId = entity.DriverId;
				var oldSecondDriverId = entity.SecondDriverId;
				var wasOpen = entity.ReturnDateTime is null;

				if (_selectedId is null)
				{
					var localDate = exitDtLocal.Date;
					var localTomorrow = localDate.AddDays(1);

					var startUtc = DateTime.SpecifyKind(localDate, DateTimeKind.Local).ToUniversalTime();
					var endUtc = DateTime.SpecifyKind(localTomorrow, DateTimeKind.Local).ToUniversalTime();

					var nextDailyNo = (await _db.VehicleMovements
						.Where(x => !x.IsDeleted &&
									x.MovementDate >= startUtc &&
									x.MovementDate < endUtc)
						.MaxAsync(x => (int?)x.DailyNo) ?? 0) + 1;

					entity.MovementDate = startUtc;
					entity.DailyNo = nextDailyNo;

                    // EKLE
                    entity.Status = "Planlandı";
                }

				entity.VehicleId = vehicleId;
				entity.DriverId = didValue;
				entity.SecondDriverId = SecondDriverCombo.SelectedValue is int sdid ? sdid : (int?)null;
				entity.VehicleCommanderId = CommanderCombo.SelectedValue is int cid ? cid : (int?)null;

				entity.VehiclePlateText = vehicleId is null ? plateText : null;
				entity.DriverText = null;
				entity.SecondDriverText = entity.SecondDriverId is null ? EmptyToNull(SecondDriverCombo.Text) : null;
				entity.CommanderText = entity.VehicleCommanderId is null ? EmptyToNull(CommanderCombo.Text) : null;

				entity.ExitDateTime = DateTime.SpecifyKind(exitDtLocal, DateTimeKind.Local).ToUniversalTime();
				entity.ReturnDateTime = returnDtLocal is null
					? null
					: DateTime.SpecifyKind(returnDtLocal.Value, DateTimeKind.Local).ToUniversalTime();

				entity.Route = EmptyToNull(RouteCombo.Text);
				entity.Purpose = EmptyToNull(DepartureCombo.Text);
				entity.Description = EmptyToNull(DutyTypeCombo.Text);

				var passenger = TryParseNullableInt(PassengerCountBox.Text);
				var load = TryParseNullableInt(LoadAmountBox.Text);
				entity.LoadOrPassengerInfo = BuildLoadOrPassengerInfo(passenger, load);

				var doneKm = TryParseNullableInt(DoneKmBox.Text);

				if (vehicleId is not null)
				{
					var vehicle = selectedVehicle ?? await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId.Value && !v.IsDeleted);

					if (vehicle is not null)
					{
						if (_selectedId is null)
						{
							entity.StartKm = vehicle.VehicleKm ?? 0;
						}
						else if (entity.StartKm is null)
						{
							entity.StartKm = vehicle.VehicleKm ?? 0;
						}

						entity.EndKm = doneKm is null
							? null
							: entity.StartKm.GetValueOrDefault() + doneKm.Value;

						if (string.IsNullOrWhiteSpace(VehicleTypeCombo.Text))
							VehicleTypeCombo.Text = vehicle.VehicleType ?? "";
					}
					else
					{
						entity.StartKm = null;
						entity.EndKm = null;
					}
				}
				else
				{
					entity.StartKm = null;
					entity.EndKm = null;
				}

				if (_selectedId is null)
					_db.VehicleMovements.Add(entity);

				// =========================
				// DURUM GÜNCELLEME BLOĞU
				// =========================

				var isNowOpen = entity.ReturnDateTime is null;

                // 1) Yeni kayıt açıldıysa
                if (_selectedId is null && isNowOpen)
                {
                    // Workflow v2
                    // Araç ve sürücü durumları artık Save sırasında değil,
                    // StartMission_Click içinde yönetilecek.

                    // if (selectedVehicle is not null)
                    //     selectedVehicle.VehicleSituation = "Görevde";

                    // if (selectedDriver is not null)
                    //     selectedDriver.DriverSituation = "Sürüş Görevi";

                    // if (selectedSecondDriver is not null)
                    //     selectedSecondDriver.DriverSituation = "Sürüş Görevi";
                }

    //            // 2) Mevcut açık kayıt düzenleniyorsa ve sürücü/araç değiştiyse
    //            if (_selectedId is not null && wasOpen && isNowOpen)
				//{
				//	// Eski araç değiştiyse eskiyi müsait yap
				//	if (oldVehicleId != entity.VehicleId && oldVehicleId is not null)
				//	{
				//		var oldVehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == oldVehicleId.Value && !v.IsDeleted);
				//		if (oldVehicle is not null && NormalizeVehicleSituation(oldVehicle.VehicleSituation) == "Görevde")
				//			oldVehicle.VehicleSituation = "Müsait";
				//	}

				//	// Yeni araç varsa görevde yap
				//	if (entity.VehicleId is not null)
				//	{
				//		var newVehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == entity.VehicleId.Value && !v.IsDeleted);
				//		if (newVehicle is not null)
				//			newVehicle.VehicleSituation = "Görevde";
				//	}

				//	// Eski 1. sürücü değiştiyse eskiyi müsait yap
				//	if (oldDriverId != entity.DriverId && oldDriverId is not null)
				//	{
				//		var oldDriver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == oldDriverId.Value && !d.IsDeleted);
				//		if (oldDriver is not null && NormalizeDriverSituation(oldDriver.DriverSituation) == "Sürüş Görevi")
				//			oldDriver.DriverSituation = "Müsait";
				//	}

				//	// Yeni 1. sürücüyü görevde yap
				//	if (entity.DriverId is not null)
				//	{
				//		var newDriver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == entity.DriverId.Value && !d.IsDeleted);
				//		if (newDriver is not null)
				//			newDriver.DriverSituation = "Sürüş Görevi";
				//	}

				//	// Eski 2. sürücü değiştiyse eskiyi müsait yap
				//	if (oldSecondDriverId != entity.SecondDriverId && oldSecondDriverId is not null)
				//	{
				//		var oldSecondDriver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == oldSecondDriverId.Value && !d.IsDeleted);
				//		if (oldSecondDriver is not null && NormalizeDriverSituation(oldSecondDriver.DriverSituation) == "Sürüş Görevi")
				//			oldSecondDriver.DriverSituation = "Müsait";
				//	}

				//	// Yeni 2. sürücüyü görevde yap
				//	if (entity.SecondDriverId is not null)
				//	{
				//		var newSecondDriver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == entity.SecondDriverId.Value && !d.IsDeleted);
				//		if (newSecondDriver is not null)
				//			newSecondDriver.DriverSituation = "Sürüş Görevi";
				//	}
				//}

				// 3) Açık kayıt kapatılıyorsa
				//if (wasOpen && !isNowOpen)
				//{
				//	if (entity.VehicleId is not null)
				//	{
				//		var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == entity.VehicleId.Value && !v.IsDeleted);
				//		if (vehicle is not null)
				//		{
				//			if (NormalizeVehicleSituation(vehicle.VehicleSituation) == "Görevde")
				//				vehicle.VehicleSituation = "Müsait";
				//		}
				//	}

				//	if (entity.DriverId is not null)
				//	{
				//		var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == entity.DriverId.Value && !d.IsDeleted);
				//		if (driver is not null && NormalizeDriverSituation(driver.DriverSituation) == "Sürüş Görevi")
				//			driver.DriverSituation = "Müsait";
				//	}

				//	if (entity.SecondDriverId is not null)
				//	{
				//		var secondDriver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == entity.SecondDriverId.Value && !d.IsDeleted);
				//		if (secondDriver is not null && NormalizeDriverSituation(secondDriver.DriverSituation) == "Sürüş Görevi")
				//			secondDriver.DriverSituation = "Müsait";
				//	}
				//}

				// 4) Kayıt kapalıysa araç km her zaman son EndKm'ye eşitlensin
				//if (entity.VehicleId is not null && entity.ReturnDateTime is not null)
				//{
				//	var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == entity.VehicleId.Value && !v.IsDeleted);
				//	if (vehicle is not null && entity.EndKm.HasValue)
				//	{
				//		vehicle.VehicleKm = entity.EndKm.Value;
				//	}
				//}

                var isNew = _selectedId is null;

                // ... entity oluşturma / güncelleme işlemleri ...
                await _db.SaveChangesAsync();

                if (isNew)
                {
                    AppLogger.Info("VehicleMovement.Save",
                        $"Sevk kaydı oluşturuldu. Id: {entity.Id}, Plaka: {entity.Vehicle?.Plate ?? entity.VehiclePlateText}");
                }
                else
                {
                    AppLogger.Info("VehicleMovement.Update",
                        $"Sevk kaydı güncellendi. Id: {entity.Id}, Plaka: {entity.Vehicle?.Plate ?? entity.VehiclePlateText}");
                }

                Notify(isNew
                    ? $"Kaydedildi: #{entity.Id}"
                    : $"Güncellendi: #{entity.Id}");

                await LoadAsync();
                ClearForm();
                PrepareNewFormState();
            }
            catch (Exception ex)
            {
                var isNew = _selectedId is null;

                AppLogger.Error(
                    isNew ? "VehicleMovement.Save" : "VehicleMovement.Update",
                    isNew ? "Sevk kaydı oluşturma hatası." : "Sevk kaydı güncelleme hatası.",
                    ex);

                Notify("Hata: kaydetme başarısız.", "Hata");
                MessageBox.Show(ex.ToString(), "Hata (detay)");
            }
        }

		private async void Delete_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (_selectedId is null)
				{
					Notify("Silmek için kayıt seç.", "Uyarı");
					return;
				}

				var confirm = MessageBox.Show("Seçili hareket silinsin mi?", "Onay", MessageBoxButton.YesNo);
				if (confirm != MessageBoxResult.Yes)
					return;

				var entity = await _db.VehicleMovements.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
				if (entity is null)
				{
					Notify("Kayıt bulunamadı.", "Uyarı");
					return;
				}

				entity.IsDeleted = true;
				await _db.SaveChangesAsync();
                // ✅ BAŞARILI LOG
                AppLogger.Info("VehicleMovement.Delete", $"Sevk kaydı silindi. Id: {_selectedId.Value}");

                Notify($"Silindi: #{_selectedId.Value}");
				await LoadAsync();
				ClearForm();
				PrepareNewFormState();
			}
			catch (Exception ex)
			{
                // ❌ HATA LOG
                AppLogger.Error("VehicleMovement.Delete", "Sevk kaydı silme hatası.", ex);
                Notify("Hata: silme başarısız.", "Hata");
				MessageBox.Show(ex.Message, "Hata");
			}
		}


        private async void StartMission_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedId == null)
            {
                Notify("Lütfen bir görev seçiniz.", "Uyarı");
                return;
            }

            var movement = await _db.VehicleMovements
                .Include(x => x.Vehicle)
                .Include(x => x.Driver)
                .Include(x => x.SecondDriver)
                .FirstOrDefaultAsync(x => x.Id == _selectedId);

            if (movement == null)
                return;

            if (movement.Status != "Planlandı")
            {
                Notify("Sadece planlanan görev başlatılabilir.", "Uyarı");
                return;
            }

            var now = DateTime.UtcNow;

            // Planlanan çıkış günü
            var plannedDate = movement.MovementDate == default
                ? movement.ExitDateTime.ToLocalTime().Date
                : movement.MovementDate.ToLocalTime().Date;

            // Gerçek çıkış günü
            var actualDate = now.ToLocalTime().Date;

            // Eğer farklı bir günde göreve başlanıyorsa
            if (plannedDate != actualDate)
            {
                var startUtc = DateTime.SpecifyKind(actualDate, DateTimeKind.Local)
                    .ToUniversalTime();

                var endUtc = DateTime.SpecifyKind(actualDate.AddDays(1), DateTimeKind.Local)
                    .ToUniversalTime();

                var nextDailyNo =
                    (await _db.VehicleMovements
                        .Where(x =>
                            !x.IsDeleted &&
                            x.Id != movement.Id &&
                            x.MovementDate >= startUtc &&
                            x.MovementDate < endUtc)
                        .MaxAsync(x => (int?)x.DailyNo) ?? 0) + 1;

                movement.MovementDate = now;
                movement.DailyNo = nextDailyNo;
            }

            // Görev bilgisi
            movement.Status = "Görevde";
            movement.ActualExitDateTime = now;

            // DİKKAT:
            // ExitDateTime planlanan çıkış zamanıdır.
            // Artık üzerine yazmıyoruz.

            // Araç durumu
            if (movement.Vehicle != null)
                movement.Vehicle.VehicleSituation = "Görevde";

            // 1. Sürücü
            if (movement.Driver != null)
                movement.Driver.DriverSituation = "Sürüş Görevi";

            // 2. Sürücü
            if (movement.SecondDriver != null)
                movement.SecondDriver.DriverSituation = "Sürüş Görevi";

            await _db.SaveChangesAsync();

            AppLogger.Info(
                "Mission.Start",
                $"Görev başlatıldı. Hareket No:{movement.Id}, Sevk No:{movement.MovementDate.ToLocalTime():yyyyMMdd}-{movement.DailyNo:000}");

            Notify("Görev başlatıldı.");

            await LoadAsync();

            await ReloadMovement(movement.Id);
        }

        private async void FinishMission_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedId == null)
            {
                Notify("Lütfen bir görev seçiniz.", "Uyarı");
                return;
            }

            var movement = await _db.VehicleMovements
                .Include(x => x.Vehicle)
                .Include(x => x.Driver)
                .Include(x => x.SecondDriver)
                .FirstOrDefaultAsync(x => x.Id == _selectedId);

            if (movement == null)
                return;

            // Sadece görevde olan kayıt bitirilebilir
            if (movement.Status != "Görevde")
            {
                Notify("Sadece görevde olan kayıtlar bitirilebilir.", "Uyarı");
                return;
            }

            // Yapılan KM kontrolü
            if (string.IsNullOrWhiteSpace(DoneKmBox.Text))
            {
                Notify("Lütfen yapılan kilometreyi giriniz.", "Uyarı");
                DoneKmBox.Focus();
                return;
            }

            if (!int.TryParse(DoneKmBox.Text.Trim(), out var doneKm) || doneKm < 0)
            {
                Notify("Yapılan kilometre değeri hatalı.", "Uyarı");
                DoneKmBox.Focus();
                return;
            }

            // Gerçek dönüş zamanı = Görev Bitir butonuna basıldığı an
            var nowUtc = DateTime.UtcNow;
            var nowLocal = nowUtc.ToLocalTime();

            // Çıkış zamanı kontrolü
            var exitLocal = movement.ExitDateTime.ToLocalTime();

            if (nowLocal < exitLocal)
            {
                Notify("Dönüş zamanı çıkış zamanından önce olamaz.", "Uyarı");
                return;
            }

            // Yapılan KM -> Son KM hesaplama
            if (movement.StartKm.HasValue)
            {
                movement.EndKm = movement.StartKm.Value + doneKm;
            }
            else
            {
                // Başlangıç KM yoksa yapılan KM'yi doğrudan son KM olarak kullanıyoruz.
                movement.EndKm = doneKm;
            }

            // Gerçek dönüş zamanı
            movement.ReturnDateTime = nowUtc;
            movement.ActualReturnDateTime = nowUtc;

            // Formdaki dönüş bilgilerini de güncelle
            ReturnDatePicker.SelectedDate = nowLocal.Date;
            ReturnTimeBox.Text = nowLocal.ToString("HH:mm");

            // Durum
            movement.Status = "Tamamlandı";

            // Araç durumu
            if (movement.Vehicle != null)
            {
                movement.Vehicle.VehicleSituation = "Müsait";
                movement.Vehicle.VehicleKm = movement.EndKm.Value;
            }

            // 1. Sürücü durumu
            if (movement.Driver != null)
                movement.Driver.DriverSituation = "Müsait";

            // 2. Sürücü durumu
            if (movement.SecondDriver != null)
                movement.SecondDriver.DriverSituation = "Müsait";

            await _db.SaveChangesAsync();

            AppLogger.Info(
                "Mission.Finish",
                $"Görev tamamlandı. " +
                $"Hareket No:{movement.Id}, " +
                $"Araç:{movement.Vehicle?.Plate}, " +
                $"Yapılan Km:{doneKm}, " +
                $"Son Km:{movement.EndKm}");

            Notify("Görev başarıyla tamamlandı.");

            await LoadAsync();

            ClearForm();

            PrepareNewFormState();
        }

        private async void CancelMission_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedId == null)
            {
                Notify("Lütfen bir görev seçiniz.", "Uyarı");
                return;
            }

            var movement = await _db.VehicleMovements
                .Include(x => x.Vehicle)
                .Include(x => x.Driver)
                .Include(x => x.SecondDriver)
                .FirstOrDefaultAsync(x => x.Id == _selectedId);

            if (movement == null)
                return;

            if (movement.Status == "Tamamlandı")
            {
                Notify("Tamamlanan görev iptal edilemez.", "Uyarı");
                return;
            }

            if (movement.Status == "İptal")
            {
                Notify("Görev zaten iptal edilmiş.", "Uyarı");
                return;
            }

            movement.Status = "İptal";
            movement.CancelDateTime = DateTime.UtcNow;

            if (movement.Vehicle != null)
                movement.Vehicle.VehicleSituation = "Müsait";

            if (movement.Driver != null)
                movement.Driver.DriverSituation = "Müsait";

            if (movement.SecondDriver != null)
                movement.SecondDriver.DriverSituation = "Müsait";



            if (movement.ActualExitDateTime != null)
            {
                movement.ActualReturnDateTime = DateTime.UtcNow;
            }

            System.Diagnostics.Debug.WriteLine(
    $"Araç durumu: {movement.Vehicle?.VehicleSituation}");

            await _db.SaveChangesAsync();

            AppLogger.Info("Mission.Cancel",
                $"Görev iptal edildi. Hareket No:{movement.Id}");

            Notify("Görev iptal edildi.");

            await LoadAsync();
            MovementsGrid.SelectedItem = null;

            _selectedId = null;
            _currentMovement = null;

            ClearForm();
            PrepareNewFormState();
        }


		private async void MovementsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MovementsGrid.SelectedItem is not VehicleMovementRow row)
				return;

			_currentMovement = await _db.VehicleMovements
				.Include(x => x.Vehicle)
				.Include(x => x.Driver)
				.Include(x => x.SecondDriver)
				.Include(x => x.VehicleCommander)
				.FirstOrDefaultAsync(x => x.Id == row.Id && !x.IsDeleted);

			if (_currentMovement == null)
				return;

			_selectedId = _currentMovement.Id;

			// Başka görevlerde halen "Görevde" olan sürücülerin ID'lerini al.
			// Düzenlenen mevcut görev kontrol dışında bırakılıyor.
	


			// 1. sürücü listesi
			ComboBoxSearchHelper.BindContains(
				DriverCombo,
				_availableDrivers,
				nameof(Driver.FullName),
				nameof(Driver.Id),
				x => x.FullName ?? "");

			// 2. sürücü olarak 1. sürücü seçilemesin.
			var secondDrivers = _availableDrivers
				.Where(x => x.Id != _currentMovement.DriverId)
				.OrderBy(x => x.FullName)
				.ToList();

			ComboBoxSearchHelper.BindContains(
				SecondDriverCombo,
				secondDrivers,
				nameof(Driver.FullName),
				nameof(Driver.Id),
				x => x.FullName ?? "");

			// Formdaki mevcut değerleri seç.
			FillForm(_currentMovement);
		}

		private void FillForm(VehicleMovement m)
        {



			VehicleCombo.SelectedValue = m.VehicleId;
            DriverCombo.SelectedValue = m.DriverId;
            SecondDriverCombo.SelectedValue = m.SecondDriverId;
            CommanderCombo.SelectedValue = m.VehicleCommanderId;

            ExitDatePicker.SelectedDate = m.ExitDateTime.ToLocalTime().Date;
            ExitTimeBox.Text = m.ExitDateTime.ToLocalTime().ToString("HH:mm");

            ReturnDatePicker.SelectedDate = m.ReturnDateTime?.ToLocalTime().Date;
            ReturnTimeBox.Text = m.ReturnDateTime is null
                ? ""
                : m.ReturnDateTime.Value.ToLocalTime().ToString("HH:mm");

            UpdateReturnHighlight();

            RouteCombo.Text = m.Route ?? "";
            DepartureCombo.Text = m.Purpose ?? "";
            DutyTypeCombo.Text = m.Description ?? "";

            var parsed = ParseLoadOrPassengerInfo(m.LoadOrPassengerInfo);

            PassengerCountBox.Text = parsed.passenger?.ToString() ?? "";
            LoadAmountBox.Text = parsed.load?.ToString() ?? "";

            if (m.StartKm.HasValue &&
                m.EndKm.HasValue &&
                m.EndKm >= m.StartKm)
            {
                DoneKmBox.Text = (m.EndKm.Value - m.StartKm.Value).ToString();
            }
            else
            {
                DoneKmBox.Text = "";
            }

            VehicleTypeCombo.Text = m.Vehicle?.VehicleType ?? "";
            StatusBox.Text = CalcStatus(m);

            DailyNoBox.Text = m.DailyNo.ToString("000");
        }
        private async Task RefreshCurrentAsync()
        {
            if (_selectedId == null)
                return;

            _currentMovement = await _db.VehicleMovements
                .Include(x => x.Vehicle)
                .Include(x => x.Driver)
                .Include(x => x.SecondDriver)
                .Include(x => x.VehicleCommander)
                .FirstOrDefaultAsync(x => x.Id == _selectedId && !x.IsDeleted);

            if (_currentMovement == null)
                return;

            FillForm(_currentMovement);

            await LoadAsync();
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var q = (SearchBox.Text ?? "").Trim().ToLowerInvariant();

			if (string.IsNullOrWhiteSpace(q))
			{
				MovementsGrid.ItemsSource = _all;
				UpdateCount(_all.Count);
				return;
			}

			var filtered = _all.Where(x =>
				(x.Driver ?? "").ToLowerInvariant().Contains(q) ||
				(x.SecondDriver ?? "").ToLowerInvariant().Contains(q) ||
				(x.Plate ?? "").ToLowerInvariant().Contains(q) ||
				(x.VehicleType ?? "").ToLowerInvariant().Contains(q) ||
				(x.Status ?? "").ToLowerInvariant().Contains(q) ||
				(x.Route ?? "").ToLowerInvariant().Contains(q) ||
				(x.Commander ?? "").ToLowerInvariant().Contains(q) ||
				(x.Departure ?? "").ToLowerInvariant().Contains(q) ||
				(x.DutyType ?? "").ToLowerInvariant().Contains(q))
				.ToList();

			MovementsGrid.ItemsSource = filtered;
			UpdateCount(filtered.Count);
		}

		private void ReturnDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateReturnHighlight();
		}

		private void ReturnTimeBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			UpdateReturnHighlight();
		}

		// =========================
		// HELPERS
		// =========================
		private void ClearForm()
		{
			MovementsGrid.SelectedItem = null;

			VehicleCombo.SelectedIndex = -1;
			DriverCombo.SelectedIndex = -1;
			SecondDriverCombo.SelectedIndex = -1;
			CommanderCombo.SelectedIndex = -1;
			RouteCombo.SelectedIndex = -1;
			DepartureCombo.SelectedIndex = -1;
			DutyTypeCombo.SelectedIndex = -1;
			VehicleTypeCombo.SelectedIndex = -1;

			DailyNoBox.Text = "";
			StatusBox.Text = "Planlandı";

			SetExitNow();
			ReturnDatePicker.SelectedDate = null;
			ReturnTimeBox.Text = "";
			UpdateReturnHighlight();

			DoneKmBox.Text = "";
			PassengerCountBox.Text = "";
			LoadAmountBox.Text = "";
		}

		private async void PrepareNewFormState()
		{
			var localToday = DateTime.Today;
			var localTomorrow = localToday.AddDays(1);

			var startUtc = DateTime.SpecifyKind(localToday, DateTimeKind.Local).ToUniversalTime();
			var endUtc = DateTime.SpecifyKind(localTomorrow, DateTimeKind.Local).ToUniversalTime();

			var nextDailyNo = (await _db.VehicleMovements
				.Where(x => !x.IsDeleted &&
							x.MovementDate >= startUtc &&
							x.MovementDate < endUtc)
				.MaxAsync(x => (int?)x.DailyNo) ?? 0) + 1;

			DailyNoBox.Text = nextDailyNo.ToString("000");
			StatusBox.Text = "Planlandı";
		}

		private void UpdateReturnHighlight()
		{
			var isReturnEmpty =
				ReturnDatePicker.SelectedDate == null &&
				string.IsNullOrWhiteSpace(ReturnTimeBox.Text);

			if (isReturnEmpty)
			{
				ReturnDatePicker.Background = new SolidColorBrush(Color.FromRgb(255, 249, 196));
				ReturnTimeBox.Background = new SolidColorBrush(Color.FromRgb(255, 249, 196));
			}
			else
			{
				ReturnDatePicker.Background = Brushes.White;
				ReturnTimeBox.Background = Brushes.White;
			}
		}

		private static string NormalizeVehicleSituation(string? value)
		{
			var v = (value ?? "").Trim();
			return string.IsNullOrWhiteSpace(v) ? "Müsait" : v;
		}

		private static string NormalizeDriverSituation(string? value)
		{
			var v = (value ?? "").Trim();
			return string.IsNullOrWhiteSpace(v) ? "Müsait" : v;
		}

		private static bool IsVehicleBlockedForDispatch(string? situation)
		{
			var s = NormalizeVehicleSituation(situation);

			return s == "Görevde"
				|| s == "Kademe"
				|| s == "Servis"
				|| s == "Fabrika";
		}

		private static bool IsDriverBlockedForDispatch(string? situation)
		{
			var s = NormalizeDriverSituation(situation);

			return s == "Sürüş Görevi"
				|| s == "İzin"
				|| s == "Görevlendirme"
				|| s == "YDGG"
				|| s == "İstirahat"
				|| s == "Birlik İçi Görev"
				|| s == "Diğer";
		}

		private static string? EmptyToNull(string? value)
		{
			var v = (value ?? "").Trim();
			return string.IsNullOrWhiteSpace(v) ? null : v;
		}

		private static int? TryParseNullableInt(string? text)
		{
			var t = (text ?? "").Trim();
			if (string.IsNullOrWhiteSpace(t)) return null;
			return int.TryParse(t, out var v) ? v : null;
		}

		private static bool TryBuildDateTime(DatePicker datePicker, TextBox timeBox, out DateTime value, out string error)
		{
			error = "";
			value = default;

			if (datePicker.SelectedDate is not DateTime date)
			{
				error = "Tarih seçilmedi.";
				return false;
			}

			var timeText = (timeBox.Text ?? "").Trim();
			if (string.IsNullOrWhiteSpace(timeText))
				timeText = "00:00";

			if (!TimeSpan.TryParse(timeText, CultureInfo.InvariantCulture, out var time))
			{
				error = "Saat formatı geçersiz. Örnek: 08:30";
				return false;
			}

			value = date.Date.Add(time);
			return true;
		}

		private static bool TryBuildNullableDateTime(DatePicker datePicker, TextBox timeBox, out DateTime? value, out string error)
		{
			error = "";
			value = null;

			if (datePicker.SelectedDate is not DateTime date)
				return true;

			var timeText = (timeBox.Text ?? "").Trim();
			if (string.IsNullOrWhiteSpace(timeText))
				timeText = "00:00";

			if (!TimeSpan.TryParse(timeText, CultureInfo.InvariantCulture, out var time))
			{
				error = "Saat formatı geçersiz. Örnek: 17:15";
				return false;
			}

			value = date.Date.Add(time);
			return true;
		}

        private static string CalcStatus(VehicleMovement movement)
        {
            switch (movement.Status)
            {
                case "Tamamlandı":
                    return "Tamamlandı";

                case "İptal":
                    return "İptal";

                case "Görevde":
                    return "Görevde";
            }

            var exit = movement.ExitDateTime.ToLocalTime();
            var diff = exit - DateTime.Now;

            if (diff.TotalMinutes > 15)
                return "Planlandı";

            if (diff.TotalMinutes >= 0)
                return "Görev Yaklaşıyor";

            return "Görev Gecikti";
        }

        private static string? BuildLoadOrPassengerInfo(int? passenger, int? load)
		{
			if (passenger is null && load is null) return null;
			return $"Yolcu:{passenger?.ToString() ?? ""};Yük:{load?.ToString() ?? ""}";
		}

		private static (int? passenger, int? load) ParseLoadOrPassengerInfo(string? text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return (null, null);

			int? passenger = null;
			int? load = null;

			var parts = text.Split(';', StringSplitOptions.RemoveEmptyEntries);
			foreach (var part in parts)
			{
				var item = part.Trim();

				if (item.StartsWith("Yolcu:", StringComparison.OrdinalIgnoreCase))
				{
					var val = item.Substring("Yolcu:".Length).Trim();
					if (int.TryParse(val, out var p))
						passenger = p;
				}
				else if (item.StartsWith("Yük:", StringComparison.OrdinalIgnoreCase))
				{
					var val = item.Substring("Yük:".Length).Trim();
					if (int.TryParse(val, out var l))
						load = l;
				}
			}

			return (passenger, load);
		}

		private static void Notify(string message, string title = "Bilgi")
		{
			MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
		}

        private static Brush GetStatusBrush(string status)
        {
            return status switch
            {
                "Planlandı" => Brushes.DodgerBlue,

                "Görev Yaklaşıyor" => Brushes.DarkOrange,

                "Görev Gecikti" => Brushes.Red,

                "Görevde" => Brushes.MediumVioletRed,

                "Tamamlandı" => Brushes.Green,

                "İptal" => Brushes.Gray,

                _ => Brushes.Black
            };
        }

		// I/O
		private void ExportDaily_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var today = DateTime.Today;

				var rows = _all
					.Where(x =>
						x.ExitDateTimeUtc.ToLocalTime().Date == today)
					.OrderBy(x => x.DailyNo)
					.ToList();

				if (!rows.Any())
				{
					Notify("Bugün için aktarılacak görev kaydı bulunamadı.", "Bilgi");
					return;
				}

				var path = ExportRowsToExcel(
					rows,
					$"Görev Kayıt Defteri_{today:yyyy-MM-dd_HH-mm}.xlsx",
					today,
					today);

				Notify($"Günlük Excel export tamamlandı.\n{path}");
			}
			catch (Exception ex)
			{
				Notify("Günlük Excel export başarısız.", "Hata");
				MessageBox.Show(ex.Message, "Hata");
			}
		}

		private void ExportMonthly_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var today = DateTime.Today;

				var startDate = new DateTime(
					today.Year,
					today.Month,
					1);

				var endDate = startDate
					.AddMonths(1)
					.AddDays(-1);

				var rows = _all
				.Where(x =>
				{
					var date = x.ExitDateTimeUtc
						.ToLocalTime()
						.Date;

					return date >= startDate &&
						   date <= endDate;
				})
				.OrderBy(x => x.ExitDateTimeUtc)
				.ThenBy(x => x.DailyNo)
				.Select((x, index) =>
				{
					x.DailyNo = index + 1;
					return x;
				})
				.ToList();

				if (!rows.Any())
				{
					Notify("Bu ay için aktarılacak görev kaydı bulunamadı.", "Bilgi");
					return;
				}

				var path = ExportRowsToExcel(
					rows,
					$"Görev Kayıt Defteri_{today:yyyy-MM_HH-mm}.xlsx",
					startDate,
					endDate);

				Notify($"Aylık Excel export tamamlandı.\n{path}");
			}
			catch (Exception ex)
			{
				Notify("Aylık Excel export başarısız.", "Hata");
				MessageBox.Show(ex.Message, "Hata");
			}
		}

		private static string ExportRowsToCsv(List<VehicleMovementRow> rows, string fileName)
		{
			var folder = @"D:\Araç Görev Kayıt Defteri";
			Directory.CreateDirectory(folder);

			var path = Path.Combine(folder, fileName);

			var sb = new StringBuilder();

			sb.AppendLine("Sıra No,Sürücü,2. Sürücü,Plaka,Çıkış Saati,Dönüş Saati,Araç Cinsi,Durum,Tarih,Güzergah,Araç Komutanı,Başkanlık,Yapılan Km,Taşınan Yolcu,Taşınan Yük,Görev Türü");

			foreach (var x in rows)
			{
				sb.AppendLine(string.Join(",",
					Csv(x.DailyNo.ToString()),
					Csv(x.Driver),
					Csv(x.SecondDriver),
					Csv(x.Plate),
					Csv(x.ExitTimeText),
					Csv(x.ReturnTimeText),
					Csv(x.VehicleType),
					Csv(x.Status),
					Csv(x.DateText),
					Csv(x.Route),
					Csv(x.Commander),
					Csv(x.Departure),
					Csv(x.KmText),
					Csv(x.PassengerCount?.ToString()),
					Csv(x.LoadAmount?.ToString()),
					Csv(x.DutyType)
				));
			}

			File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
			return path;
		}

		private static string Csv(string? value)
		{
			var s = value ?? "";
			s = s.Replace("\"", "\"\"");
			return $"\"{s}\"";
		}

		private static string ExportRowsToExcel(
	List<VehicleMovementRow> rows,
	string fileName,
	DateTime? startDate,
	DateTime? endDate)
		{
			var folder = @"D:\Görev Kayıt Defteri";
			Directory.CreateDirectory(folder);

			var path = Path.Combine(folder, fileName);

			using var wb = new XLWorkbook();

			var ws = wb.Worksheets.Add("Araç Sevk Raporu");

			// =========================================================
			// RAPOR ÖZETİ
			// =========================================================

			var totalKm = rows
				.Where(x => x.DoneKm.HasValue)
				.Sum(x => x.DoneKm!.Value);

			var totalDuration = TimeSpan.Zero;

			foreach (var row in rows)
			{
				var end = row.ReturnDateTimeUtc ?? DateTime.UtcNow;

				if (end >= row.ExitDateTimeUtc)
				{
					totalDuration +=
						end - row.ExitDateTimeUtc;
				}
			}

			var totalHours =
				(int)totalDuration.TotalHours;

			var startText =
				startDate?.ToString("dd.MM.yyyy") ?? "—";

			var endText =
				endDate?.ToString("dd.MM.yyyy") ?? "—";

			var reportTitle =
				$"{startText} - {endText} TARİHLERİ ARASI ARAÇ SEVK RAPORU";

			var summaryText =
				$"Toplam Kayıt: {rows.Count}   |   " +
				$"Toplam KM: {totalKm:N0} km   |   " +
				$"Toplam Görev Süresi: {totalHours} sa {totalDuration.Minutes} dk";

			// =========================================================
			// BAŞLIK
			// =========================================================

			ws.Range(1, 1, 1, 16).Merge();

			ws.Cell(1, 1).Value =
				reportTitle;

			ws.Range(1, 1, 1, 16).Style.Font.Bold = true;

			ws.Range(1, 1, 1, 16).Style.Font.FontSize = 16;

			ws.Range(1, 1, 1, 16).Style.Alignment.Horizontal =
				XLAlignmentHorizontalValues.Center;

			ws.Range(1, 1, 1, 16).Style.Alignment.Vertical =
				XLAlignmentVerticalValues.Center;

			ws.Row(1).Height = 28;

			// =========================================================
			// ÖZET
			// =========================================================

			ws.Range(2, 1, 2, 16).Merge();

			ws.Cell(2, 1).Value =
				summaryText;

			ws.Range(2, 1, 2, 16).Style.Font.Bold = true;

			ws.Range(2, 1, 2, 16).Style.Alignment.Horizontal =
				XLAlignmentHorizontalValues.Center;

			ws.Range(2, 1, 2, 16).Style.Alignment.Vertical =
				XLAlignmentVerticalValues.Center;

			ws.Row(2).Height = 22;

			// =========================================================
			// SÜTUN BAŞLIKLARI
			// =========================================================

			int headerRow = 4;

			ws.Cell(headerRow, 1).Value = "Sıra No";
			ws.Cell(headerRow, 2).Value = "Sürücü";
			ws.Cell(headerRow, 3).Value = "2. Sürücü";
			ws.Cell(headerRow, 4).Value = "Plaka";
			ws.Cell(headerRow, 5).Value = "Çıkış Saati";
			ws.Cell(headerRow, 6).Value = "Dönüş Saati";
			ws.Cell(headerRow, 7).Value = "Araç Cinsi";
			ws.Cell(headerRow, 8).Value = "Araç Marka";
			ws.Cell(headerRow, 9).Value = "Durum";
			ws.Cell(headerRow, 10).Value = "Tarih";
			ws.Cell(headerRow, 11).Value = "Güzergah";
			ws.Cell(headerRow, 12).Value = "Araç Komutanı";
			ws.Cell(headerRow, 13).Value = "Başkanlık";
			ws.Cell(headerRow, 14).Value = "Yapılan Km";
			ws.Cell(headerRow, 15).Value = "Görev Süresi";
			ws.Cell(headerRow, 16).Value = "Görev Türü";

			var headerRange =
				ws.Range(
					headerRow,
					1,
					headerRow,
					16);

			headerRange.Style.Font.Bold = true;

			headerRange.Style.Alignment.Horizontal =
				XLAlignmentHorizontalValues.Center;

			headerRange.Style.Alignment.Vertical =
				XLAlignmentVerticalValues.Center;

			headerRange.Style.Alignment.WrapText = true;

			// =========================================================
			// VERİLER
			// =========================================================

			int excelRow = headerRow + 1;

			foreach (var x in rows)
			{
				ws.Cell(excelRow, 1).Value =
					x.DailyNo;

				ws.Cell(excelRow, 2).Value =
					x.Driver ?? "";

				ws.Cell(excelRow, 3).Value =
					x.SecondDriver ?? "";

				ws.Cell(excelRow, 4).Value =
					x.Plate ?? "";

				ws.Cell(excelRow, 5).Value =
					x.ExitTimeText ?? "";

				ws.Cell(excelRow, 6).Value =
					x.ReturnTimeText ?? "";

				ws.Cell(excelRow, 7).Value =
					x.VehicleType ?? "";

				// Araç marka
				ws.Cell(excelRow, 8).Value =
					x.VehicleBrand ?? "";

				ws.Cell(excelRow, 9).Value =
					x.Status ?? "";

				ws.Cell(excelRow, 10).Value =
					x.DateText ?? "";

				ws.Cell(excelRow, 11).Value =
					x.Route ?? "";

				ws.Cell(excelRow, 12).Value =
					x.Commander ?? "";

				ws.Cell(excelRow, 13).Value =
					x.Departure ?? "";

				ws.Cell(excelRow, 14).Value =
					x.DoneKm.HasValue
						? x.DoneKm.Value
						: "";

				// Görev süresi
				var end =
					x.ReturnDateTimeUtc ?? DateTime.UtcNow;

				var duration =
					end >= x.ExitDateTimeUtc
						? end - x.ExitDateTimeUtc
						: TimeSpan.Zero;

				ws.Cell(excelRow, 15).Value =
					$"{(int)duration.TotalHours} sa {duration.Minutes} dk";

				ws.Cell(excelRow, 16).Value =
					x.DutyType ?? "";

				excelRow++;
			}

			// =========================================================
			// TABLO BİÇİMLENDİRME
			// =========================================================

			var lastRow =
				Math.Max(
					excelRow - 1,
					headerRow);

			var tableRange =
				ws.Range(
					headerRow,
					1,
					lastRow,
					16);

			tableRange.Style.Border.OutsideBorder =
				XLBorderStyleValues.Thin;

			tableRange.Style.Border.InsideBorder =
				XLBorderStyleValues.Thin;

			tableRange.Style.Alignment.Vertical =
				XLAlignmentVerticalValues.Center;

			// =========================================================
			// FİLTRE
			// =========================================================

			tableRange.SetAutoFilter();

			// =========================================================
			// SÜTUN GENİŞLİKLERİ
			// =========================================================

			ws.Columns().AdjustToContents();

			// Çok uzun kolonların aşırı genişlemesini engelle
			ws.Column(2).Width = 24;   // Sürücü
			ws.Column(3).Width = 24;   // 2. Sürücü
			ws.Column(7).Width = 18;   // Araç Cinsi
			ws.Column(8).Width = 18;   // Araç Marka
			ws.Column(11).Width = 24;  // Güzergah
			ws.Column(12).Width = 24;  // Araç Komutanı
			ws.Column(13).Width = 24;  // Başkanlık
			ws.Column(15).Width = 16;  // Görev Süresi
			ws.Column(16).Width = 20;  // Görev Türü

			// =========================================================
			// SAYFA AYARLARI
			// =========================================================

			ws.PageSetup.PageOrientation =
				XLPageOrientation.Landscape;

			ws.PageSetup.PaperSize =
				XLPaperSize.A4Paper;

			// Tüm sütunlar tek sayfa genişliğine sığsın
			ws.PageSetup.PagesWide = 1;

			// Dikeyde sayfa sınırı yok
			ws.PageSetup.PagesTall = 0;

			// Kenar boşlukları
			ws.PageSetup.Margins.Left = 0.25;
			ws.PageSetup.Margins.Right = 0.25;
			ws.PageSetup.Margins.Top = 0.5;
			ws.PageSetup.Margins.Bottom = 0.5;

			// Her sayfada sütun başlıkları tekrar etsin
			ws.PageSetup.SetRowsToRepeatAtTop(
				headerRow,
				headerRow);

			// İlk 4 satırı sabitle
			ws.SheetView.FreezeRows(headerRow);

			wb.SaveAs(path);

			return path;
		}

		private void SetExitNow()
		{
			var now = DateTime.Now;
			ExitDatePicker.SelectedDate = now.Date;
			ExitTimeBox.Text = now.ToString("HH:mm");
		}



        private async void VehicleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (VehicleCombo.SelectedValue is not int vehicleId)
                    return;

                var vehicle = await _db.Vehicles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == vehicleId);

                if (vehicle is null)
                    return;

                // ARAÇ TİPİ ALANININ ADINA GÖRE BUNU KULLAN
                VehicleTypeCombo.Text = vehicle.VehicleType ?? "";
            }
            catch (Exception ex)
            {
                AppLogger.Error("VehicleMovements.VehicleCombo_SelectionChanged",
                    "Plaka seçilince araç tipi doldurma hatası.", ex);
            }
        }

		//Tüm kayıtları göster
        private async void ShowAllCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            await LoadAsync();
        }

        private async Task ReloadMovement(int movementId)
        {
            // Şimdilik boş bırak.
        }


        /// <summary>
        /// Geliştirici aracı.
        /// Araç ve sürücü durumlarını mevcut görev kayıtlarından yeniden oluşturur.
        /// Normal çalışma sırasında çağrılmaz.
        /// </summary>
        private async Task RepairMissionStatesAsync()
        {

            var vehicles = await _db.Vehicles
						.Where(x => !x.IsDeleted)
						.ToListAsync();

            foreach (var vehicle in vehicles)
            {
                vehicle.VehicleSituation = "Müsait";
            }

            var drivers = await _db.Drivers
						.Where(x => !x.IsDeleted)
						.ToListAsync();

            foreach (var driver in drivers)
            {
                driver.DriverSituation = "Müsait";
            }

            var activeMovements = await _db.VehicleMovements
						.Include(x => x.Vehicle)
						.Include(x => x.Driver)
						.Include(x => x.SecondDriver)
						.Where(x => !x.IsDeleted)
						.Where(x => x.Status == "Görevde")
						.ToListAsync();
            foreach (var movement in activeMovements)
            {
                if (movement.Vehicle != null)
                    movement.Vehicle.VehicleSituation = "Görevde";

                if (movement.Driver != null)
                    movement.Driver.DriverSituation = "Sürüş Görevi";

                if (movement.SecondDriver != null)
                    movement.SecondDriver.DriverSituation = "Sürüş Görevi";
            }

            await _db.SaveChangesAsync();

            //Notify("Araç ve sürücü durumları yeniden oluşturuldu.");

            //Notify("Repair tamamlandı.");
        }

        private void DriverCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			
			try
            {
                if (_availableDrivers == null || _availableDrivers.Count == 0)
                    return;

                var selectedDriverId =
                    DriverCombo.SelectedValue is int id
                        ? id
                        : (int?)null;

                var secondDrivers = _availableDrivers
                    .Where(x => selectedDriverId == null || x.Id != selectedDriverId.Value)
                    .OrderBy(x => x.FullName)
                    .ToList();

                var previousSecondDriverId =
                    SecondDriverCombo.SelectedValue is int secondId
                        ? secondId
                        : (int?)null;

                ComboBoxSearchHelper.BindContains(
                    SecondDriverCombo,
                    secondDrivers,
                    nameof(Driver.FullName),
                    nameof(Driver.Id),
                    x => x.FullName ?? "");

                

                // Önceden seçili 2. sürücü hâlâ uygunsa seçimini koru
                if (previousSecondDriverId.HasValue &&
                    secondDrivers.Any(x => x.Id == previousSecondDriverId.Value))
                {
                    SecondDriverCombo.SelectedValue = previousSecondDriverId.Value;
                }
                else
                {
                    SecondDriverCombo.SelectedIndex = -1;
                }

            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    "VehicleMovements.DriverCombo_SelectionChanged",
                    "1. sürücü değişirken 2. sürücü listesi güncellenemedi.",
                    ex);
            }

        }

		private async Task CheckMonthlyReportAsync()
		{
			try
			{
				var today = DateTime.Today;

				// Geçen ay
				var previousMonth = today.AddMonths(-1);

				var startDate = new DateTime(
					previousMonth.Year,
					previousMonth.Month,
					1);

				var endDate = startDate.AddMonths(1);

				var folder = @"D:\Görev Kayıt Defteri";
				Directory.CreateDirectory(folder);

				// Örnek:
				// Görev Kayıt Defteri_2026-08.xlsx
				var fileName =
					$"Görev Kayıt Defteri_{previousMonth:yyyy-MM}.xlsx";

				var path = Path.Combine(folder, fileName);

				// Rapor daha önce oluşturulmuşsa tekrar oluşturma
				if (File.Exists(path))
					return;

				// Geçen aya ait TÜM sevk kayıtlarını DB'den al.
				// Ekrandaki _all listesini kullanmıyoruz.
				var movements = await _db.VehicleMovements
					.AsNoTracking()
					.Where(x =>
						!x.IsDeleted &&
						x.ExitDateTime >= startDate.ToUniversalTime() &&
						x.ExitDateTime < endDate.ToUniversalTime())
					.Include(x => x.Vehicle)
					.Include(x => x.Driver)
					.Include(x => x.SecondDriver)
					.Include(x => x.VehicleCommander)
					.OrderBy(x => x.ExitDateTime)
					.ThenBy(x => x.DailyNo)
					.ToListAsync();

				// O ay hiç görev yoksa dosya oluşturma
				if (movements.Count == 0)
					return;

				var rows = movements.Select(m =>
				{
					var exitLocal = m.ExitDateTime.ToLocalTime();
					var returnLocal = m.ReturnDateTime?.ToLocalTime();

					var parsed =
						ParseLoadOrPassengerInfo(m.LoadOrPassengerInfo);

					var status = CalcStatus(m);

					int? doneKm = null;

					if (m.StartKm.HasValue &&
						m.EndKm.HasValue &&
						m.EndKm.Value >= m.StartKm.Value)
					{
						doneKm = m.EndKm.Value - m.StartKm.Value;
					}

					var dateForNo =
						m.MovementDate == default
							? exitLocal.Date
							: m.MovementDate.ToLocalTime().Date;

					return new VehicleMovementRow
					{
						Id = m.Id,

						MovementNo =
							$"{dateForNo:yyyyMMdd}-{m.DailyNo:000}",

						DailyNo = m.DailyNo,

						Driver =
							m.Driver?.FullName ?? m.DriverText,

						SecondDriver =
							m.SecondDriver?.FullName ?? m.SecondDriverText,

						Plate =
							m.Vehicle?.Plate ??
							m.VehiclePlateText ??
							"",

						ExitTimeText =
							exitLocal.ToString("HH:mm"),

						ReturnTimeText =
							returnLocal is null
								? "—"
								: returnLocal.Value.ToString("HH:mm"),

						VehicleType =
							m.Vehicle?.VehicleType,

						Status = status,

						StatusBrush =
							GetStatusBrush(status),

						DateText =
							exitLocal.ToString("dd.MM.yyyy"),

						Route = m.Route,

						Commander =
							m.VehicleCommander?.FullName ??
							m.CommanderText,

						Departure =
							m.Purpose,

						DoneKm =
							doneKm,

						PassengerCount =
							parsed.passenger,

						LoadAmount =
							parsed.load,

						DutyType =
							m.Description,

						ExitDateTimeUtc =
							m.ExitDateTime,

						ReturnDateTimeUtc =
							m.ReturnDateTime,

						IsPreviousDayOpen = false
					};
				}).ToList();

				// Sorgulama ekranındaki Excel formatını kullan
				ExportRowsToExcel(
					rows,
					fileName,
					startDate,
					endDate);
			}
			catch (Exception ex)
			{
				AppLogger.Error(
					"VehicleMovements.MonthlyReport",
					"Otomatik aylık rapor oluşturulamadı.",
					ex);
			}
		}



	}


}