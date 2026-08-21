using FleetManagement.Desktop.Services;
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
	public partial class DriversPage : Page
	{
		private readonly AppDbContext _db = new(App.DbOptions);

		private int? _selectedId;
		private List<Driver> _allDrivers = new();

		public DriversPage()
		{
			InitializeComponent();
            LoadDriverSituations();
            Loaded += async (_, __) => await LoadDriversAsync();
		}

		private async Task LoadDriversAsync()
		{
			try
			{
				var list = await _db.Drivers
					.AsNoTracking()
					.Where(x => !x.IsDeleted)
					.OrderByDescending(x => x.Id)
					.ToListAsync();

				_allDrivers = list;
				DriversGrid.ItemsSource = _allDrivers;

				// İstersen sessiz kalsın:
				// Notify($"Yüklendi: {_allDrivers.Count} kayıt");
				FilterInfo.Text = $"Toplam kayıt: {_allDrivers.Count}";
			}
			catch (Exception ex)
			{
				Notify("Hata: sürücüler yüklenemedi.", "Hata");
				MessageBox.Show(ex.Message, "Hata");
			}
		}

		private void DriversGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (DriversGrid.SelectedItem is not Driver d)
				return;

			_selectedId = d.Id;

			DriverNumberBox.Text = d.DriverNumber ?? "";
			FullNameBox.Text = d.FullName ?? "";
			PhoneBox.Text = d.PhoneNumber ?? "";

            DriverSituationCombo.Text = d.DriverSituation ?? "";
            IsExternalCheckBox.IsChecked = d.IsExternal;

            // İstersen bunu da sessiz yap:
            // Notify($"Seçildi: #{d.Id}");
        }

		private async void Refresh_Click(object sender, RoutedEventArgs e)
		{
			await LoadDriversAsync();
			Notify("Liste yenilendi.");
		}

		private void New_Click(object sender, RoutedEventArgs e)
		{
			ClearForm();
			// Notify("Yeni kayıt için form hazır.");
		}

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var isNew = _selectedId is null;

                var driverNumber = (DriverNumberBox.Text ?? "").Trim();
                var fullName = (FullNameBox.Text ?? "").Trim();
                var phone = (PhoneBox.Text ?? "").Trim();
                var situation = (DriverSituationCombo.Text ?? "").Trim();

                if (string.IsNullOrWhiteSpace(driverNumber))
                {
                    Notify("Sürücü No zorunlu.", "Uyarı");
                    return;
                }

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    Notify("Ad Soyad zorunlu.", "Uyarı");
                    return;
                }

                if (string.IsNullOrWhiteSpace(situation))
                {
                    Notify("Sürücü durumu zorunlu.", "Uyarı");
                    return;
                }

                Driver entity;

                if (isNew)
                {
                    entity = new Driver
                    {
                        DriverNumber = driverNumber,
                        FullName = fullName,
                        DriverSituation = string.IsNullOrWhiteSpace(situation)
                                ? "Müsait"
                                : situation,
                        PhoneNumber = phone,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false,
                        IsExternal = IsExternalCheckBox.IsChecked == true
                    };

                    _db.Drivers.Add(entity);
                }
                else
                {
                    entity = await _db.Drivers.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                    if (entity is null)
                    {
                        Notify("Kayıt bulunamadı (yenileyin).", "Uyarı");
                        return;
                    }

                    entity.DriverNumber = driverNumber;
                    entity.FullName = fullName;
                    entity.DriverSituation = string.IsNullOrWhiteSpace(situation)
                        ? "Müsait"
                        : situation;
                    entity.PhoneNumber = phone;
                    entity.IsExternal = IsExternalCheckBox.IsChecked == true;
                }

                await _db.SaveChangesAsync();

                if (isNew)
                {
                    AppLogger.Info("Drivers.Save",
                        $"Sürücü kaydedildi. Id: {entity.Id}, Ad: {entity.FullName}");
                }
                else
                {
                    AppLogger.Info("Drivers.Update",
                        $"Sürücü güncellendi. Id: {entity.Id}, Ad: {entity.FullName}");
                }

                Notify(isNew
                    ? $"Kaydedildi: #{entity.Id}"
                    : $"Güncellendi: #{entity.Id}");

                await LoadDriversAsync();
                ClearForm();
            }
            catch (DbUpdateException dbex)
            {
                AppLogger.Error(
                    _selectedId is null ? "Drivers.Save" : "Drivers.Update",
                    _selectedId is null
                        ? "Sürücü kaydetme sırasında DB hatası oluştu."
                        : "Sürücü güncelleme sırasında DB hatası oluştu.",
                    dbex);

                Notify("Hata: kayıt yapılamadı (muhtemelen Sürücü No tekrar ediyor).", "DB Hatası");
                MessageBox.Show(dbex.InnerException?.Message ?? dbex.Message, "DB Hatası");
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    _selectedId is null ? "Drivers.Save" : "Drivers.Update",
                    _selectedId is null
                        ? "Sürücü kaydetme hatası."
                        : "Sürücü güncelleme hatası.",
                    ex);

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

                var confirm = MessageBox.Show("Seçili sürücü silinsin mi?", "Onay", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes)
                    return;

                var entity = await _db.Drivers.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                if (entity is null)
                {
                    Notify("Kayıt bulunamadı (yenileyin).", "Uyarı");
                    return;
                }

                entity.IsDeleted = true;
                await _db.SaveChangesAsync();

                AppLogger.Info("Drivers.Delete",
                    $"Sürücü silindi. Id: {_selectedId.Value}, Ad: {entity.FullName}");

                Notify($"Silindi: #{_selectedId.Value}");

                await LoadDriversAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Drivers.Delete", "Sürücü silme hatası.", ex);

                Notify("Hata: silme başarısız.", "Hata");
                MessageBox.Show(ex.Message, "Hata");
            }
        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = (SearchBox.Text ?? "").Trim().ToLowerInvariant();

            var filtered = _allDrivers
                .Where(x =>
                    (x.FullName ?? "").ToLowerInvariant().Contains(text) ||
                    (x.DriverNumber ?? "").ToLowerInvariant().Contains(text) ||
                    (x.PhoneNumber ?? "").ToLowerInvariant().Contains(text))
                .ToList();

            DriversGrid.ItemsSource = filtered;

            FilterInfo.Text = $"Toplam kayıt: {filtered.Count}";
        }

        private void ClearForm()
		{
			_selectedId = null;
			DriversGrid.SelectedItem = null;

			DriverNumberBox.Text = "";
			FullNameBox.Text = "";
			PhoneBox.Text = "";
			SearchBox.Text = "";
            DriverSituationCombo.SelectedIndex = -1;
            DriverSituationCombo.Text = "";

            IsExternalCheckBox.IsChecked = false;
        }

		private static void Notify(string message, string title = "Bilgi")
		{
			// İstersen tamamen sessize almak için:
			// return;

			MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
		}
        private void LoadDriverSituations()
        {
            DriverSituationCombo.ItemsSource = new List<string>
			{
				"Müsait",
				"Sürüş Görevi",
				"İzin",
				"Görevlendirme",
				"YDGG",
				"İstirahat",
				"Birlik İçi Görev",
				"Diğer"
			};
        }
    }
}