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
    public partial class VehicleCommandersPage : Page
    {
        private readonly AppDbContext _db = new(App.DbOptions);

        private int? _selectedId;
        private List<VehicleCommander> _all = new();

        public VehicleCommandersPage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var list = await _db.VehicleCommanders
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                _all = list;
                CommandersGrid.ItemsSource = _all;
                FilterInfo.Text = $"Toplam kayıt: {_all.Count}";
            }
            catch (Exception ex)
            {
                Notify("Hata: komutanlar yüklenemedi.", "Hata");
                MessageBox.Show(ex.Message, "Hata");
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadAsync();
            Notify("Liste yenilendi.");
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            Notify("Yeni kayıt için form hazır.");
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var number = (CommanderNumberBox.Text ?? "").Trim();
                var fullName = (FullNameBox.Text ?? "").Trim();
                var phone = (PhoneBox.Text ?? "").Trim();
                var unitName = (UnitNameBox.Text ?? "").Trim();

                if (string.IsNullOrWhiteSpace(number) || string.IsNullOrWhiteSpace(fullName))
                {
                    Notify("Komutan No ve Ad Soyad zorunludur.", "Uyarı");
                    return;
                }

                var exists = await _db.VehicleCommanders.AsNoTracking()
                    .AnyAsync(x => !x.IsDeleted
                                   && x.CommanderNumber.ToLower() == number.ToLower()
                                   && (_selectedId == null || x.Id != _selectedId.Value));

                if (exists)
                {
                    Notify("Bu Komutan No zaten var.", "Uyarı");
                    return;
                }

                if (_selectedId is null)
                {
                    var entity = new VehicleCommander
                    {
                        CommanderNumber = number,
                        FullName = fullName,
                        PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone,
                        UnitName = string.IsNullOrWhiteSpace(unitName) ? null : unitName,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    _db.VehicleCommanders.Add(entity);
                    await _db.SaveChangesAsync();
                    Notify($"Kaydedildi: #{entity.Id}");
                }
                else
                {
                    var entity = await _db.VehicleCommanders.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                    if (entity is null)
                    {
                        Notify("Kayıt bulunamadı (yenileyin).", "Uyarı");
                        return;
                    }

                    entity.CommanderNumber = number;
                    entity.FullName = fullName;
                    entity.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone;
                    entity.UnitName = string.IsNullOrWhiteSpace(unitName) ? null : unitName;

                    await _db.SaveChangesAsync();
                    Notify($"Güncellendi: #{entity.Id}");
                }

                await LoadAsync();
                ClearForm();
            }
            catch (DbUpdateException dbex)
            {
                Notify("Hata: kayıt yapılamadı (kısıt/tekrar olabilir).", "DB Hatası");
                MessageBox.Show(dbex.InnerException?.Message ?? dbex.Message, "DB Hatası");
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

                var confirm = MessageBox.Show("Seçili komutan silinsin mi?", "Onay", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes)
                    return;

                var entity = await _db.VehicleCommanders.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                if (entity is null)
                {
                    Notify("Kayıt bulunamadı (yenileyin).", "Uyarı");
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

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            Notify("Temizlendi");
        }

        private void CommandersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CommandersGrid.SelectedItem is not VehicleCommander x)
                return;

            _selectedId = x.Id;

            CommanderNumberBox.Text = x.CommanderNumber ?? "";
            FullNameBox.Text = x.FullName ?? "";
            PhoneBox.Text = x.PhoneNumber ?? "";
            UnitNameBox.Text = x.UnitName ?? "";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = (SearchBox.Text ?? "").Trim().ToLowerInvariant();
            var total = _all.Count;

            if (string.IsNullOrWhiteSpace(q))
            {
                CommandersGrid.ItemsSource = _all;
                FilterInfo.Text = $"Toplam kayıt: {total}";
                return;
            }

            var filtered = _all
                .Where(x =>
                    (x.CommanderNumber ?? "").ToLowerInvariant().Contains(q) ||
                    (x.FullName ?? "").ToLowerInvariant().Contains(q) ||
                    (x.PhoneNumber ?? "").ToLowerInvariant().Contains(q) ||
                    (x.UnitName ?? "").ToLowerInvariant().Contains(q))
                .ToList();

            CommandersGrid.ItemsSource = filtered;
            FilterInfo.Text = $"Toplam kayıt: {filtered.Count} / {total}";
        }

        private void ClearForm()
        {
            _selectedId = null;
            CommandersGrid.SelectedItem = null;

            CommanderNumberBox.Text = "";
            FullNameBox.Text = "";
            PhoneBox.Text = "";
            UnitNameBox.Text = "";
            SearchBox.Text = "";
        }

        private static void Notify(string message, string title = "Bilgi")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}