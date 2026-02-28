using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

using FleetManagement.Infrastructure.Data;
using FleetManagement.Domain.Entities;

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
            Loaded += async (_, __) => await LoadDriversAsync();
        }

        private async Task LoadDriversAsync()
        {
            try
            {
                FormInfo.Text = "Yükleniyor...";

                var list = await _db.Drivers
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                _allDrivers = list;
                DriversGrid.ItemsSource = _allDrivers;

                FormInfo.Text = $"Yüklendi: {_allDrivers.Count} kayıt";
            }
            catch (Exception ex)
            {
                FormInfo.Text = "Hata: sürücüler yüklenemedi.";
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

            FormInfo.Text = $"Seçildi: #{d.Id}";
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDriversAsync();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            FormInfo.Text = "Yeni kayıt için form hazır.";
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var driverNumber = (DriverNumberBox.Text ?? "").Trim();
                var fullName = (FullNameBox.Text ?? "").Trim();
                var phone = (PhoneBox.Text ?? "").Trim();

                if (string.IsNullOrWhiteSpace(driverNumber))
                {
                    FormInfo.Text = "Sürücü No zorunlu.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    FormInfo.Text = "Ad Soyad zorunlu.";
                    return;
                }

                if (_selectedId is null)
                {
                    // INSERT
                    var entity = new Driver
                    {
                        DriverNumber = driverNumber,
                        FullName = fullName,
                        PhoneNumber = phone,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    _db.Drivers.Add(entity);
                    await _db.SaveChangesAsync();

                    FormInfo.Text = $"Kaydedildi: #{entity.Id}";
                }
                else
                {
                    // UPDATE
                    var entity = await _db.Drivers.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                    if (entity is null)
                    {
                        FormInfo.Text = "Kayıt bulunamadı (yenileyin).";
                        return;
                    }

                    entity.DriverNumber = driverNumber;
                    entity.FullName = fullName;
                    entity.PhoneNumber = phone;

                    await _db.SaveChangesAsync();

                    FormInfo.Text = $"Güncellendi: #{entity.Id}";
                }

                await LoadDriversAsync();
                ClearForm();
            }
            catch (DbUpdateException dbex)
            {
                // DriverNumber unique index -> aynı numara girilirse buraya düşer
                FormInfo.Text = "Hata: kayıt yapılamadı (muhtemelen Sürücü No tekrar ediyor).";
                MessageBox.Show(dbex.InnerException?.Message ?? dbex.Message, "DB Hatası");
            }
            catch (Exception ex)
            {
                FormInfo.Text = "Hata: kaydetme başarısız.";
                MessageBox.Show(ex.Message, "Hata");
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedId is null)
                {
                    FormInfo.Text = "Silmek için listeden kayıt seç.";
                    return;
                }

                var confirm = MessageBox.Show("Seçili sürücü silinsin mi?", "Onay", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes)
                    return;

                var entity = await _db.Drivers.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                if (entity is null)
                {
                    FormInfo.Text = "Kayıt bulunamadı (yenileyin).";
                    return;
                }

                // SOFT DELETE (IsDeleted var)
                entity.IsDeleted = true;
                await _db.SaveChangesAsync();

                FormInfo.Text = $"Silindi: #{_selectedId.Value}";

                await LoadDriversAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                FormInfo.Text = "Hata: silme başarısız.";
                MessageBox.Show(ex.Message, "Hata");
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            FormInfo.Text = "Form temizlendi.";
			SearchBox.Text = "";
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var q = (SearchBox.Text ?? "").Trim().ToLowerInvariant();
			var total = _allDrivers.Count;

			if (string.IsNullOrWhiteSpace(q))
			{
				DriversGrid.ItemsSource = _allDrivers;
				FilterInfo.Text = $"Filtre: yok (Toplam {total})";
				return;
			}

			var filtered = _allDrivers
				.Where(x =>
					(x.DriverNumber ?? "").ToLowerInvariant().Contains(q) ||
					(x.FullName ?? "").ToLowerInvariant().Contains(q) ||
					(x.PhoneNumber ?? "").ToLowerInvariant().Contains(q))
				.ToList();

			DriversGrid.ItemsSource = filtered;
			FilterInfo.Text = $"Filtre: \"{q}\" → {filtered.Count} / {total} kayıt";
		}

		private void ClearForm()
        {
            _selectedId = null;
            DriversGrid.SelectedItem = null;

            DriverNumberBox.Text = "";
            FullNameBox.Text = "";
            PhoneBox.Text = "";
			SearchBox.Text = "";
		}
    }
}