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
    public partial class RoutesPage : Page
    {
        private readonly AppDbContext _db = new(App.DbOptions);

        private int? _selectedId;
        private List<Route> _all = new();

        public RoutesPage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var list = await _db.Routes
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                _all = list;
                RoutesGrid.ItemsSource = _all;
                FilterInfo.Text = $"Toplam kayıt: {_all.Count}";
            }
            catch (Exception ex)
            {
                Notify("Hata: rotalar yüklenemedi.", "Hata");
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
                var code = (CodeBox.Text ?? "").Trim();
                var name = (NameBox.Text ?? "").Trim();

                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                {
                    Notify("Kod ve Rota Adı zorunludur.", "Uyarı");
                    return;
                }

                var exists = await _db.Routes.AsNoTracking()
                    .AnyAsync(x => !x.IsDeleted
                                   && x.Code.ToLower() == code.ToLower()
                                   && (_selectedId == null || x.Id != _selectedId.Value));

                if (exists)
                {
                    Notify("Bu kod zaten var.", "Uyarı");
                    return;
                }

                if (_selectedId is null)
                {
                    var entity = new Route
                    {
                        Code = code,
                        Name = name,
                        Description = EmptyToNull(DescBox.Text),
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    _db.Routes.Add(entity);
                    await _db.SaveChangesAsync();

                    Notify($"Kaydedildi: #{entity.Id}");
                }
                else
                {
                    var entity = await _db.Routes.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                    if (entity is null)
                    {
                        Notify("Kayıt bulunamadı (yenileyin).", "Uyarı");
                        return;
                    }

                    entity.Code = code;
                    entity.Name = name;
                    entity.Description = EmptyToNull(DescBox.Text);

                    await _db.SaveChangesAsync();

                    Notify($"Güncellendi: #{entity.Id}");
                }

                await LoadAsync();
                ClearForm();
            }
            catch (DbUpdateException dbex)
            {
                Notify("Hata: kayıt yapılamadı (muhtemelen Kod tekrar ediyor).", "DB Hatası");
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

                var confirm = MessageBox.Show("Seçili rota silinsin mi?", "Onay", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes)
                    return;

                var entity = await _db.Routes.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
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

        private void RoutesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoutesGrid.SelectedItem is not Route x)
                return;

            _selectedId = x.Id;

            CodeBox.Text = x.Code ?? "";
            NameBox.Text = x.Name ?? "";
            DescBox.Text = x.Description ?? "";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = (SearchBox.Text ?? "").Trim().ToLowerInvariant();
            var total = _all.Count;

            if (string.IsNullOrWhiteSpace(q))
            {
                RoutesGrid.ItemsSource = _all;
                FilterInfo.Text = $"Toplam kayıt: {total}";
                return;
            }

            var filtered = _all
                .Where(x =>
                    (x.Code ?? "").ToLowerInvariant().Contains(q) ||
                    (x.Name ?? "").ToLowerInvariant().Contains(q) ||
                    (x.StartPoint ?? "").ToLowerInvariant().Contains(q) ||
                    (x.EndPoint ?? "").ToLowerInvariant().Contains(q) ||
                    (x.Description ?? "").ToLowerInvariant().Contains(q))
                .ToList();

            RoutesGrid.ItemsSource = filtered;
            FilterInfo.Text = $"Toplam kayıt: {filtered.Count} / {total}";
        }

        private void ClearForm()
        {
            _selectedId = null;
            RoutesGrid.SelectedItem = null;

            CodeBox.Text = "";
            NameBox.Text = "";
            DescBox.Text = "";
            SearchBox.Text = "";
        }

        private static string? EmptyToNull(string? value)
        {
            var v = (value ?? "").Trim();
            return string.IsNullOrWhiteSpace(v) ? null : v;
        }

        private static void Notify(string message, string title = "Bilgi")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}