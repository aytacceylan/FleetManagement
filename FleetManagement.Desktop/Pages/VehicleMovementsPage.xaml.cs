using System;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
    public partial class VehicleMovementsPage : Page
    {
        public VehicleMovementsPage()
        {
            InitializeComponent();

            ExitDatePicker.SelectedDate = DateTime.Today;
            ExitTimeBox.Text = "08:00";
            ReturnTimeBox.Text = "17:00";


            // Sunum modu: boş liste/combobox
            MovementsGrid.ItemsSource = Array.Empty<object>();
            VehicleCombo.ItemsSource = Array.Empty<object>();
            DriverCombo.ItemsSource = Array.Empty<object>();
            CommanderCombo.ItemsSource = Array.Empty<object>();
        }

        private void RefreshMovements_Click(object sender, RoutedEventArgs e)
        {
            MovementInfo.Text = "Yenilendi (yakında DB bağlanacak).";
        }

        private void SaveMovement_Click(object sender, RoutedEventArgs e)
        {
            if (!TryBuildDateTime(ExitDatePicker, ExitTimeBox, out var exitDt, out var err1))
            {
                MovementInfo.Text = "Çıkış zamanı hatalı: " + err1;
                return;
            }

            if (!TryBuildNullableDateTime(ReturnDatePicker, ReturnTimeBox, out var returnDt, out var err2))
            {
                MovementInfo.Text = "Dönüş zamanı hatalı: " + err2;
                return;
            }

            if (returnDt is not null && returnDt < exitDt)
            {
                MovementInfo.Text = "Dönüş zamanı çıkıştan önce olamaz.";
                return;
            }

            // Şimdilik sadece ekrana yazdırıyoruz (DB bağlanınca buradan entity'ye set edeceğiz)
            var retText = returnDt is null ? "(dönüş yok)" : returnDt.Value.ToString("dd.MM.yyyy HH:mm");
            MovementInfo.Text = $"OK ✅ Çıkış: {exitDt:dd.MM.yyyy HH:mm} | Dönüş: {retText}";
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

            // Return boş bırakılabilir: tarih seçilmediyse null kabul
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

    }
}