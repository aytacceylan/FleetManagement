using ClosedXML.Excel;
using FleetManagement.Desktop.Dtos;
using FleetManagement.Desktop.Services;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
    public partial class VehicleMovementReportsPage : Page
    {
        private readonly AppDbContext _db = new(App.DbOptions);
        private List<VehicleMovementRow> _all = new();
        private List<VehicleMovementRow> _filtered = new();

        public VehicleMovementReportsPage()
        {
            InitializeComponent();

            Loaded += async (_, __) =>
            {
                StartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
                EndDatePicker.SelectedDate = DateTime.Today;

                await LoadLookupsAsync();
                await LoadAllAsync();
                ApplyFilters();
            };
        }

        private async System.Threading.Tasks.Task LoadLookupsAsync()
        {
            var plates = await _db.Vehicles.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Plate)
                .Select(x => x.Plate)
                .ToListAsync();

            PlateCombo.ItemsSource = plates;

            var drivers = await _db.Drivers.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.FullName)
                .Select(x => x.FullName)
                .ToListAsync();

            DriverCombo.ItemsSource = drivers;

            var dutyTypes = await _db.DutyTypes.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToListAsync();

            DutyTypeCombo.ItemsSource = dutyTypes;

            var units = await _db.Units.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToListAsync();

            UnitCombo.ItemsSource = units;

            var routes = await _db.Routes.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToListAsync();

            RouteCombo.ItemsSource = routes;

            var brands = await _db.Vehicles.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Select(x => x.VehicleBrand)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            VehicleBrandCombo.ItemsSource = brands;
        }

        private async System.Threading.Tasks.Task LoadAllAsync()
        {
            var raw = await _db.VehicleMovements.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Include(x => x.Vehicle)
                .Include(x => x.Driver)
                .Include(x => x.VehicleCommander)
                .Include(x => x.SecondDriver)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            var rows = raw.Select(m =>
            {
                var exitLocal = m.ExitDateTime.ToLocalTime();
                var returnLocal = m.ReturnDateTime?.ToLocalTime();
                var parsed = ParseLoadOrPassengerInfo(m.LoadOrPassengerInfo);
                var status = CalcStatus(m.ExitDateTime, m.ReturnDateTime);

                int? doneKm = null;
                if (m.StartKm.HasValue && m.EndKm.HasValue && m.EndKm.Value >= m.StartKm.Value)
                    doneKm = m.EndKm.Value - m.StartKm.Value;

                return new VehicleMovementRow
                {
                    Id = m.Id,
                    DailyNo = m.DailyNo,
                    MovementNo = $"{m.MovementDate:yyyyMMdd}-{m.DailyNo:000}",

                    Driver = m.Driver?.FullName ?? m.DriverText,
                    SecondDriver = m.SecondDriver?.FullName ?? m.SecondDriverText,
                    Plate = m.Vehicle?.Plate ?? m.VehiclePlateText ?? "",

                    ExitTimeText = exitLocal.ToString("HH:mm"),
                    ReturnTimeText = returnLocal is null ? "—" : returnLocal.Value.ToString("HH:mm"),

                    VehicleType = GetVehicleTypeSafe(m.Vehicle),

                    VehicleBrand = m.Vehicle?.VehicleBrand,

                    Status = status,
                    DateText = exitLocal.ToString("dd.MM.yyyy"),
                    Route = m.Route,
                    Commander = m.VehicleCommander?.FullName ?? m.CommanderText,
                    Departure = m.Purpose,
                    DoneKm = doneKm,
                    PassengerCount = parsed.passenger,
                    LoadAmount = parsed.load,
                    DutyType = m.Description,
                    ExitDateTimeUtc = m.ExitDateTime,
                    ReturnDateTimeUtc = m.ReturnDateTime

                };
            }).ToList();

            _all = rows;
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadLookupsAsync();
            await LoadAllAsync();
            ApplyFilters();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
            EndDatePicker.SelectedDate = DateTime.Today;

            PlateCombo.Text = "";
            DriverCombo.Text = "";
            DutyTypeCombo.Text = "";
            UnitCombo.Text = "";
            RouteCombo.Text = "";
            VehicleBrandCombo.Text = "";

            MinKmTextBox.Text = "";
            MaxKmTextBox.Text = "";

            MinDurationTextBox.Text = "";
            MaxDurationTextBox.Text = "";

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var start = StartDatePicker.SelectedDate?.Date;
            var end = EndDatePicker.SelectedDate?.Date;

            var plate = (PlateCombo.Text ?? "").Trim().ToLowerInvariant();
            var driver = (DriverCombo.Text ?? "").Trim().ToLowerInvariant();
            var dutyType = (DutyTypeCombo.Text ?? "").Trim().ToLowerInvariant();
            var unit = (UnitCombo.Text ?? "").Trim().ToLowerInvariant();
            var route = (RouteCombo.Text ?? "").Trim().ToLowerInvariant();
            var brand = (VehicleBrandCombo.Text ?? "")
              .Trim()
              .ToLowerInvariant();

            var minKm = ParseNullableInt(MinKmTextBox.Text);
            var maxKm = ParseNullableInt(MaxKmTextBox.Text);

            var minDurationHours = ParseNullableDecimal(MinDurationTextBox.Text);
            var maxDurationHours = ParseNullableDecimal(MaxDurationTextBox.Text);



            var query = _all.AsEnumerable();

            if (start.HasValue)
                query = query.Where(x => x.ExitDateTimeUtc.ToLocalTime().Date >= start.Value);

            if (end.HasValue)
                query = query.Where(x => x.ExitDateTimeUtc.ToLocalTime().Date <= end.Value);

            if (!string.IsNullOrWhiteSpace(plate))
                query = query.Where(x => (x.Plate ?? "").ToLowerInvariant().Contains(plate));

            if (!string.IsNullOrWhiteSpace(driver))
                query = query.Where(x => (x.Driver ?? "").ToLowerInvariant().Contains(driver));

            if (!string.IsNullOrWhiteSpace(dutyType))
                query = query.Where(x => (x.DutyType ?? "").ToLowerInvariant().Contains(dutyType));

            if (!string.IsNullOrWhiteSpace(route))
                query = query.Where(x => (x.Route ?? "").ToLowerInvariant().Contains(route));

            if (!string.IsNullOrWhiteSpace(brand))
            {
                query = query.Where(x =>
                    (x.VehicleBrand ?? "")
                    .ToLowerInvariant()
                    .Contains(brand));
            }

            // Yapılan KM filtresi
            if (minKm.HasValue)
            {
                query = query.Where(x =>
                    x.DoneKm.HasValue &&
                    x.DoneKm.Value >= minKm.Value);
            }

            if (maxKm.HasValue)
            {
                query = query.Where(x =>
                    x.DoneKm.HasValue &&
                    x.DoneKm.Value <= maxKm.Value);
            }

            // Görev süresi filtresi
            if (minDurationHours.HasValue)
            {
                query = query.Where(x =>
                    GetDurationHours(x) >= minDurationHours.Value);
            }

            if (maxDurationHours.HasValue)
            {
                query = query.Where(x =>
                    GetDurationHours(x) <= maxDurationHours.Value);
            }

            if (!string.IsNullOrWhiteSpace(unit))
            {
                var vehicleMap = _db.Vehicles.AsNoTracking()
                    .Where(v => !v.IsDeleted)
                    .Select(v => new { v.Plate, v.VehicleUnit })
                    .ToList();

                query = query.Where(x =>
                    vehicleMap.Any(v =>
                        v.Plate == x.Plate &&
                        (v.VehicleUnit ?? "").ToLowerInvariant().Contains(unit)));
            }

            _filtered = query
                .OrderByDescending(x => x.ExitDateTimeUtc)
                .ToList();

            // Filtrelenmiş sonuçlara yeniden sıra numarası ver
            for (int i = 0; i < _filtered.Count; i++)
            {
                _filtered[i].ReportNo = i + 1;
            }

            ResultsGrid.ItemsSource = _filtered;

            UpdateResultSummary();
        }

        private void UpdateResultSummary()
        {
            var totalKm = _filtered
                .Where(x => x.DoneKm.HasValue)
                .Sum(x => x.DoneKm!.Value);

            var totalDuration = TimeSpan.Zero;

            foreach (var row in _filtered)
            {
                var end = row.ReturnDateTimeUtc ?? DateTime.UtcNow;

                if (end >= row.ExitDateTimeUtc)
                {
                    totalDuration += end - row.ExitDateTimeUtc;
                }
            }

            var totalHours = (int)totalDuration.TotalHours;

            ResultInfoText.Text =
                $"Toplam kayıt: {_filtered.Count}   |   " +
                $"Toplam KM: {totalKm:N0} km   |   " +
                $"Toplam görev süresi: {totalHours} sa {totalDuration.Minutes} dk";
        }

        private static string CalcStatus(DateTime exitUtc, DateTime? returnUtc)
        {
            if (returnUtc is not null)
                return "Tamamlandı";

            var exitLocal = exitUtc.ToLocalTime();
            var now = DateTime.Now;

            if (exitLocal > now)
                return "Planlandı";

            return "Devam Ediyor";
        }

        private static decimal GetDurationHours(VehicleMovementRow row)
        {
            var exit = row.ExitDateTimeUtc.ToLocalTime();

            var returnTime = row.ReturnDateTimeUtc?.ToLocalTime();

            var end = returnTime ?? DateTime.Now;

            if (end < exit)
                return 0;

            var duration = end - exit;

            return (decimal)duration.TotalHours;
        }

        private static string? GetVehicleTypeSafe(Domain.Entities.Vehicle? v)
        {
            if (v is null) return null;
            return v.VehicleType;
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

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedRows = ResultsGrid.SelectedItems
                    .OfType<VehicleMovementRow>()
                    .ToList();

                var rowsToExport = selectedRows.Any() ? selectedRows : _filtered;

                if (rowsToExport == null || !rowsToExport.Any())
                {
                    MessageBox.Show("Dışa aktarılacak kayıt yok.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var now = DateTime.Now;
                var path = ExportRowsToExcel(
    rowsToExport,
    $"SevkSorgulama_{now:yyyy-MM-dd_HH-mm}.xlsx",
    StartDatePicker.SelectedDate,
    EndDatePicker.SelectedDate);
                AppLogger.Info("VehicleMovementReports.Export",
                                $"Excel export alındı. Kayıt sayısı: {rowsToExport.Count}");

                var msg = selectedRows.Any()
                    ? $"Seçili kayıtlar Excel'e aktarıldı.\n{path}"
                    : $"Filtrelenmiş kayıtlar Excel'e aktarıldı.\n{path}";

                MessageBox.Show(msg, "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("VehicleMovementReportsPage.ExportExcel_Click", "Excel export hatası.", ex);
                MessageBox.Show(ex.Message, "Export Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static int? ParseNullableInt(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (int.TryParse(text.Trim(), out var value))
                return value;

            return null;
        }

        private static decimal? ParseNullableDecimal(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.Trim();

            if (decimal.TryParse(
                text,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.CurrentCulture,
                out var value))
            {
                return value;
            }

            if (decimal.TryParse(
                text,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out value))
            {
                return value;
            }

            return null;
        }

        private static string ExportRowsToExcel(List<VehicleMovementRow> rows,string fileName,DateTime? startDate, DateTime? endDate)
        {
            var folder = @"D:\Raporlar Sorgulamalar";
            Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, fileName);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Sevk Sorgulama");

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
                    totalDuration += end - row.ExitDateTimeUtc;
                }
            }

            var totalHours = (int)totalDuration.TotalHours;

            var startText = startDate?.ToString("dd.MM.yyyy") ?? "—";
            var endText = endDate?.ToString("dd.MM.yyyy") ?? "—";

            var reportTitle =
                $"{startText} - {endText} TARİHLERİ ARASI ARAÇ SEVK SORGULAMA RAPORU";

            var summaryText =
                $"Toplam Kayıt: {rows.Count}   |   " +
                $"Toplam KM: {totalKm:N0} km   |   " +
                $"Toplam Görev Süresi: {totalHours} sa {totalDuration.Minutes} dk";

            // Başlık
            ws.Range(1, 1, 1, 16).Merge();
            ws.Cell(1, 1).Value = reportTitle;

            ws.Range(1, 1, 1, 16).Style.Font.Bold = true;
            ws.Range(1, 1, 1, 16).Style.Font.FontSize = 16;
            ws.Range(1, 1, 1, 16).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;
            ws.Range(1, 1, 1, 16).Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            ws.Row(1).Height = 30;

            ws.Range(1, 1, 1, 16).Style.Font.Bold = true;
            ws.Range(1, 1, 1, 16).Style.Font.FontSize = 16;
            ws.Range(1, 1, 1, 16).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;
            ws.Range(1, 1, 1, 16).Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            ws.Row(1).Height = 28;

            // Özet
            ws.Range(2, 1, 2, 16).Merge();
            ws.Cell(2, 1).Value = summaryText;

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

            var headerRange = ws.Range(headerRow, 1, headerRow, 16);

            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            // =========================================================
            // VERİLER
            // =========================================================

            int excelRow = headerRow + 1;

            foreach (var x in rows)
            {
                ws.Cell(excelRow, 1).Value = x.ReportNo;
                ws.Cell(excelRow, 2).Value = x.Driver ?? "";
                ws.Cell(excelRow, 3).Value = x.SecondDriver ?? "";
                ws.Cell(excelRow, 4).Value = x.Plate ?? "";
                ws.Cell(excelRow, 5).Value = x.ExitTimeText ?? "";
                ws.Cell(excelRow, 6).Value = x.ReturnTimeText ?? "";
                ws.Cell(excelRow, 7).Value = x.VehicleType ?? "";
                ws.Cell(excelRow, 8).Value = x.VehicleBrand ?? "";
                ws.Cell(excelRow, 9).Value = x.Status ?? "";
                ws.Cell(excelRow, 10).Value = x.DateText ?? "";
                ws.Cell(excelRow, 11).Value = x.Route ?? "";
                ws.Cell(excelRow, 12).Value = x.Commander ?? "";
                ws.Cell(excelRow, 13).Value = x.Departure ?? "";
                ws.Cell(excelRow, 14).Value = x.DoneKm.HasValue
    ? x.DoneKm.Value
    : "";
                ws.Cell(excelRow, 15).Value = x.DurationText;
                ws.Cell(excelRow, 16).Value = x.DutyType ?? "";

                excelRow++;
            }

            // =========================================================
            // TABLO BİÇİMLENDİRME
            // =========================================================

            var lastRow = Math.Max(excelRow - 1, headerRow);

            var tableRange = ws.Range(
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

            headerRange.Style.Alignment.WrapText = true;

            // Filtre
            tableRange.SetAutoFilter();

            // Sütun genişlikleri
            ws.Columns().AdjustToContents();

            // =========================================================
            // SAYFA AYARLARI
            // =========================================================

            ws.PageSetup.PageOrientation =
                XLPageOrientation.Landscape;

            ws.PageSetup.PaperSize =
                XLPaperSize.A4Paper;

            // Tüm sütunları tek sayfa genişliğine sığdır
            ws.PageSetup.PagesWide = 1;

            // Dikeyde sayfa sınırı yok
            ws.PageSetup.PagesTall = 0;

            // Kenar boşlukları
            ws.PageSetup.Margins.Left = 0.25;
            ws.PageSetup.Margins.Right = 0.25;
            ws.PageSetup.Margins.Top = 0.5;
            ws.PageSetup.Margins.Bottom = 0.5;

            // Her sayfada sütun başlıkları tekrar etsin
            ws.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);

            // İlk satırı sabitle
            ws.SheetView.FreezeRows(headerRow);

            wb.SaveAs(path);

            return path;
        }


    }
}