using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
    public partial class VehicleMovementsPage : Page
    {
        private readonly AppDbContext _db = new(App.DbOptions);

        private int? _selectedId;
        private List<MovementRow> _all = new();

        private sealed class MovementRow
        {
            public int Id { get; set; }

            public string? VehicleDisplay { get; set; }
            public string? DriverDisplay { get; set; }
            public string? CommanderDisplay { get; set; }

            public string? Route { get; set; }
            public string? Purpose { get; set; }

            public DateTime ExitDateTime { get; set; }
            public DateTime? ReturnDateTime { get; set; }

            public string ExitDateTimeText => ExitDateTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            public string ReturnDateTimeText => ReturnDateTime is null ? "—" : ReturnDateTime.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

            public int? StartKm { get; set; }
            public int? EndKm { get; set; }
            public string KmText => $"{(StartKm?.ToString() ?? "—")} → {(EndKm?.ToString() ?? "—")}";
        }

        public VehicleMovementsPage()
        {
            InitializeComponent();

            ExitDatePicker.SelectedDate = DateTime.Today;
            ExitTimeBox.Text = "08:00";
            ReturnTimeBox.Text = "17:00";

            Loaded += async (_, __) =>
            {
                await LoadLookupsAsync();
                await LoadAsync();
            };
        }

        // =========================
        // LOAD LOOKUPS
        // =========================
        private async Task LoadLookupsAsync()
        {
            var vehicles = await _db.Vehicles.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Plate)
                .Select(x => new { x.Id, Display = x.Plate })
                .ToListAsync();

            VehicleCombo.ItemsSource = vehicles;
            VehicleCombo.DisplayMemberPath = "Display";
            VehicleCombo.SelectedValuePath = "Id";

            var drivers = await _db.Drivers.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.FullName)
                .Select(x => new { x.Id, Display = x.FullName })
                .ToListAsync();

            DriverCombo.ItemsSource = drivers;
            DriverCombo.DisplayMemberPath = "Display";
            DriverCombo.SelectedValuePath = "Id";

            var commanders = await _db.VehicleCommanders.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.FullName)
                .Select(x => new { x.Id, Display = x.FullName })
                .ToListAsync();

            CommanderCombo.ItemsSource = commanders;
            CommanderCombo.DisplayMemberPath = "Display";
            CommanderCombo.SelectedValuePath = "Id";
        }

        // =========================
        // LOAD LIST
        // =========================
        private async Task LoadAsync()
        {
            var list = await _db.VehicleMovements.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Include(x => x.Vehicle)
                .Include(x => x.Driver)
                .Include(x => x.VehicleCommander)
                .OrderByDescending(x => x.Id)
                .Select(x => new MovementRow
                {
                    Id = x.Id,
                    VehicleDisplay = x.Vehicle != null ? x.Vehicle.Plate : x.VehiclePlateText,
                    DriverDisplay = x.Driver != null ? x.Driver.FullName : x.DriverText,
                    CommanderDisplay = x.VehicleCommander != null ? x.VehicleCommander.FullName : x.CommanderText,
                    Route = x.Route,
                    Purpose = x.Purpose,
                    ExitDateTime = x.ExitDateTime,
                    ReturnDateTime = x.ReturnDateTime,
                    StartKm = x.StartKm,
                    EndKm = x.EndKm
                })
                .ToListAsync();

            _all = list;
            MovementsGrid.ItemsSource = _all;
            UpdateCount(_all.Count);
        }

        private void UpdateCount(int count) => FilterInfo.Text = $"Toplam kayıt: {count}";

        // =========================
        // UI EVENTS
        // =========================
        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadLookupsAsync();
            await LoadAsync();
            Notify("Yenilendi");
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            Notify("Yeni kayıt için form hazır");
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            Notify("Temizlendi");
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryBuildDateTime(ExitDatePicker, ExitTimeBox, out var exitDt, out var err1))
                {
                    Notify("Çıkış zamanı hatalı: " + err1, "Uyarı");
                    return;
                }

                if (!TryBuildNullableDateTime(ReturnDatePicker, ReturnTimeBox, out var returnDt, out var err2))
                {
                    Notify("Dönüş zamanı hatalı: " + err2, "Uyarı");
                    return;
                }

                if (returnDt is not null && returnDt < exitDt)
                {
                    Notify("Dönüş zamanı çıkıştan önce olamaz.", "Uyarı");
                    return;
                }

                // entity
                var entity = _selectedId is null
                    ? new VehicleMovement { CreatedAt = DateTime.UtcNow, IsDeleted = false }
                    : await _db.VehicleMovements.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);

                if (entity is null)
                {
                    Notify("Kayıt bulunamadı.", "Uyarı");
                    return;
                }

                // FK seçilirse ID bas, seçilmezse serbest metin bas
                entity.VehicleId = VehicleCombo.SelectedValue is int vid ? vid : (int?)null;
                entity.DriverId = DriverCombo.SelectedValue is int did ? did : (int?)null;
                entity.VehicleCommanderId = CommanderCombo.SelectedValue is int cid ? cid : (int?)null;

                entity.VehiclePlateText = entity.VehicleId is null ? EmptyToNull(VehiclePlateTextBox.Text) : null;
                entity.DriverText = entity.DriverId is null ? EmptyToNull(DriverTextBox.Text) : null;
                entity.CommanderText = entity.VehicleCommanderId is null ? EmptyToNull(CommanderTextBox.Text) : null;

                entity.ExitDateTime = DateTime.SpecifyKind(exitDt, DateTimeKind.Utc);
                entity.ReturnDateTime = returnDt is null ? null : DateTime.SpecifyKind(returnDt.Value, DateTimeKind.Utc);

                entity.Route = EmptyToNull(RouteBox.Text);
                entity.Purpose = EmptyToNull(PurposeBox.Text);
                entity.Description = EmptyToNull(DescBox.Text);
                entity.LoadOrPassengerInfo = EmptyToNull(LoadOrPassengerBox.Text);

                entity.StartKm = TryParseNullableInt(StartKmBox.Text);
                entity.EndKm = TryParseNullableInt(EndKmBox.Text);

                if (_selectedId is null)
                    _db.VehicleMovements.Add(entity);

                await _db.SaveChangesAsync();

                Notify(_selectedId is null ? $"Kaydedildi: #{entity.Id}" : $"Güncellendi: #{entity.Id}");
                await LoadAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                Notify("Hata: kaydetme başarısız.", "Hata");
                MessageBox.Show(ex.Message, "Hata");
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedId is null)
                {
                    Notify("Silmek için listeden kayıt seç.", "Uyarı");
                    return;
                }

                var confirm = MessageBox.Show("Seçili hareket silinsin mi?", "Onay", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes) return;

                var entity = await _db.VehicleMovements.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                if (entity is null)
                {
                    Notify("Kayıt bulunamadı.", "Uyarı");
                    return;
                }

                entity.IsDeleted = true;
                await _db.SaveChangesAsync();

                Notify($"Silindi: #{_selectedId.Value}");
                await LoadAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                Notify("Hata: silme başarısız.", "Hata");
                MessageBox.Show(ex.Message, "Hata");
            }
        }

        private async void MovementsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MovementsGrid.SelectedItem is not MovementRow row) return;

            var m = await _db.VehicleMovements.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == row.Id && !x.IsDeleted);

            if (m is null) return;

            _selectedId = m.Id;

            VehicleCombo.SelectedValue = m.VehicleId;
            DriverCombo.SelectedValue = m.DriverId;
            CommanderCombo.SelectedValue = m.VehicleCommanderId;

            VehiclePlateTextBox.Text = m.VehiclePlateText ?? "";
            DriverTextBox.Text = m.DriverText ?? "";
            CommanderTextBox.Text = m.CommanderText ?? "";

            ExitDatePicker.SelectedDate = m.ExitDateTime.ToLocalTime().Date;
            ExitTimeBox.Text = m.ExitDateTime.ToLocalTime().ToString("HH:mm");

            ReturnDatePicker.SelectedDate = m.ReturnDateTime?.ToLocalTime().Date;
            ReturnTimeBox.Text = m.ReturnDateTime is null ? "17:00" : m.ReturnDateTime.Value.ToLocalTime().ToString("HH:mm");

            RouteBox.Text = m.Route ?? "";
            PurposeBox.Text = m.Purpose ?? "";
            DescBox.Text = m.Description ?? "";
            LoadOrPassengerBox.Text = m.LoadOrPassengerInfo ?? "";

            StartKmBox.Text = m.StartKm?.ToString() ?? "";
            EndKmBox.Text = m.EndKm?.ToString() ?? "";

            Notify($"Seçildi: #{m.Id}");
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
                (x.VehicleDisplay ?? "").ToLowerInvariant().Contains(q) ||
                (x.DriverDisplay ?? "").ToLowerInvariant().Contains(q) ||
                (x.CommanderDisplay ?? "").ToLowerInvariant().Contains(q) ||
                (x.Route ?? "").ToLowerInvariant().Contains(q) ||
                (x.Purpose ?? "").ToLowerInvariant().Contains(q))
                .ToList();

            MovementsGrid.ItemsSource = filtered;
            UpdateCount(filtered.Count);
        }

        // =========================
        // HELPERS
        // =========================
        private void ClearForm()
        {
            _selectedId = null;
            MovementsGrid.SelectedItem = null;

            VehicleCombo.SelectedIndex = -1;
            DriverCombo.SelectedIndex = -1;
            CommanderCombo.SelectedIndex = -1;

            VehiclePlateTextBox.Text = "";
            DriverTextBox.Text = "";
            CommanderTextBox.Text = "";

            ExitDatePicker.SelectedDate = DateTime.Today;
            ExitTimeBox.Text = "08:00";
            ReturnDatePicker.SelectedDate = null;
            ReturnTimeBox.Text = "17:00";

            RouteBox.Text = "";
            PurposeBox.Text = "";
            DescBox.Text = "";
            LoadOrPassengerBox.Text = "";

            StartKmBox.Text = "";
            EndKmBox.Text = "";

            SearchBox.Text = "";
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

            if (!TimeSpan.TryParse(timeText, out var time))
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

            if (!TimeSpan.TryParse(timeText, out var time))
            {
                error = "Dönüş saat formatı geçersiz. Örnek: 17:15";
                return false;
            }

            value = date.Date.Add(time);
            return true;
        }

        private static void Notify(string message, string title = "Bilgi")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}