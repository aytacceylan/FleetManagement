using ClosedXML.Excel;
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

            var types = await _db.VehicleTypes.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToListAsync();

            VehicleTypeCombo.ItemsSource = types;
        }

        private async System.Threading.Tasks.Task LoadAllAsync()
        {
            var raw = await _db.VehicleMovements.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Include(x => x.Vehicle)
                .Include(x => x.Driver)
                .Include(x => x.VehicleCommander)
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
                    Plate = m.Vehicle?.Plate ?? m.VehiclePlateText ?? "",
                    ExitTimeText = exitLocal.ToString("HH:mm"),
                    ReturnTimeText = returnLocal is null ? "—" : returnLocal.Value.ToString("HH:mm"),

                    VehicleType = GetVehicleTypeSafe(m.Vehicle),

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
            VehicleTypeCombo.Text = "";

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
            var type = (VehicleTypeCombo.Text ?? "").Trim().ToLowerInvariant();

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

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(x => (x.VehicleType ?? "").ToLowerInvariant().Contains(type));

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

            ResultsGrid.ItemsSource = _filtered;
            ResultInfoText.Text = $"Toplam kayıt: {_filtered.Count}";
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var now = DateTime.Now;
                var path = ExportRowsToExcel(_filtered, $"VehicleMovementReport_{now:yyyy-MM-dd_HH-mm}.xlsx");
                MessageBox.Show($"Excel export tamamlandı.\n{path}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Export Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private static string ExportRowsToExcel(List<VehicleMovementRow> rows, string fileName)
        {
            var folder = @"C:\FleetReports";
            Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, fileName);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Araç Hareket Sorgu");

            ws.Cell(1, 1).Value = "Sıra No";
            ws.Cell(1, 2).Value = "Tarih";
            ws.Cell(1, 3).Value = "Plaka";
            ws.Cell(1, 4).Value = "Sürücü";
            ws.Cell(1, 5).Value = "Araç Tipi";
            ws.Cell(1, 6).Value = "Güzergah";
            ws.Cell(1, 7).Value = "Görev Türü";
            ws.Cell(1, 8).Value = "Başkanlık";
            ws.Cell(1, 9).Value = "Durum";
            ws.Cell(1, 10).Value = "Yapılan Km";
            ws.Cell(1, 11).Value = "Taşınan Yolcu";
            ws.Cell(1, 12).Value = "Taşınan Yük";

            int row = 2;
            foreach (var x in rows)
            {
                ws.Cell(row, 1).Value = x.DailyNo;
                ws.Cell(row, 2).Value = x.DateText ?? "";
                ws.Cell(row, 3).Value = x.Plate ?? "";
                ws.Cell(row, 4).Value = x.Driver ?? "";
                ws.Cell(row, 5).Value = x.VehicleType ?? "";
                ws.Cell(row, 6).Value = x.Route ?? "";
                ws.Cell(row, 7).Value = x.DutyType ?? "";
                ws.Cell(row, 8).Value = x.Departure ?? "";
                ws.Cell(row, 9).Value = x.Status ?? "";
                ws.Cell(row, 10).Value = x.KmText ?? "";
                ws.Cell(row, 11).Value = x.PassengerCount?.ToString() ?? "";
                ws.Cell(row, 12).Value = x.LoadAmount?.ToString() ?? "";
                row++;
            }

            var headerRange = ws.Range(1, 1, 1, 12);
            headerRange.Style.Font.Bold = true;

            var tableRange = ws.Range(1, 1, Math.Max(row - 1, 1), 12);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            tableRange.SetAutoFilter();

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            wb.SaveAs(path);
            return path;
        }
    }
}