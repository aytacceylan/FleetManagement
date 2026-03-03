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
    public partial class DutyTypesPage : Page
    {
        private readonly AppDbContext _db = new(App.DbOptions);

        private int? _selectedId;
        private List<DutyType> _all = new();

        public DutyTypesPage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var list = await _db.DutyTypes
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                _all = list;
                Grid.ItemsSource = _all;
                FilterInfo.Text = $"Toplam kayıt: {_all.Count}";
            }
            catch (Exception ex)
            {
                Notify("Görev türleri yüklenemedi.", "Hata");
                MessageBox.Show(ex.Message, "Hata");
            }
        }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Grid.SelectedItem is not DutyType x) return;

            _selectedId = x.Id;
            CodeBox.Text = x.Code ?? "";
            NameBox.Text = x.Name ?? "";
            DescBox.Text = x.Description ?? "";
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadAsync();
            Notify("Yenilendi");
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
                var code = EmptyToNull(CodeBox.Text);   // opsiyonel
                var name = (NameBox.Text ?? "").Trim();
                var desc = EmptyToNull(DescBox.Text);

                if (string.IsNullOrWhiteSpace(name))
                {
                    Notify("Görev Türü Adı zorunlu.");
                    return;
                }

                // ✅ NAME UNIQUE kontrolü (soft delete hariç)
                var nameExists = await _db.DutyTypes
                    .AsNoTracking()
                    .AnyAsync(x => !x.IsDeleted
                                   && x.Name == name
                                   && (_selectedId == null || x.Id != _selectedId.Value));

                if (nameExists)
                {
                    Notify("Bu Görev Türü Adı zaten var.");
                    return;
                }

                // (Opsiyonel) ✅ Code girildiyse UNIQUE kontrolü de istersen kalsın
                if (code is not null)
                {
                    var codeExists = await _db.DutyTypes
                        .AsNoTracking()
                        .AnyAsync(x => !x.IsDeleted
                                       && x.Code == code
                                       && (_selectedId == null || x.Id != _selectedId.Value));

                    if (codeExists)
                    {
                        Notify("Bu Kod zaten var.");
                        return;
                    }
                }

                if (_selectedId is null)
                {
                    var entity = new DutyType
                    {
                        Code = code,
                        Name = name,
                        Description = desc,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    _db.DutyTypes.Add(entity);
                    await _db.SaveChangesAsync();
                    Notify($"Kaydedildi: #{entity.Id}");
                }
                else
                {
                    var entity = await _db.DutyTypes.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                    if (entity is null)
                    {
                        Notify("Kayıt bulunamadı.");
                        return;
                    }

                    entity.Code = code;
                    entity.Name = name;
                    entity.Description = desc;

                    await _db.SaveChangesAsync();
                    Notify($"Güncellendi: #{entity.Id}");
                }

                await LoadAsync();
                ClearForm();
            }
            catch (DbUpdateException dbex)
            {
                // DB tarafında unique index varsa buraya da düşebilir
                Notify("Hata: kayıt yapılamadı (tekrar/kısıt olabilir).", "DB Hatası");
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
                    Notify("Silmek için listeden kayıt seç.");
                    return;
                }

                var confirm = MessageBox.Show("Seçili görev türü silinsin mi?", "Onay", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes) return;

                var entity = await _db.DutyTypes.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                if (entity is null)
                {
                    Notify("Kayıt bulunamadı (yenileyin).");
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
            SearchBox.Text = "";
            Notify("Temizlendi");
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = (SearchBox.Text ?? "").Trim().ToLowerInvariant();
            var total = _all.Count;

            if (string.IsNullOrWhiteSpace(q))
            {
                Grid.ItemsSource = _all;
                FilterInfo.Text = $"Toplam kayıt: {total}";
                return;
            }

            var filtered = _all.Where(x =>
                    (x.Code ?? "").ToLowerInvariant().Contains(q) ||
                    (x.Name ?? "").ToLowerInvariant().Contains(q) ||
                    (x.Description ?? "").ToLowerInvariant().Contains(q))
                .ToList();

            Grid.ItemsSource = filtered;
            FilterInfo.Text = $"{filtered.Count} / {total} kayıt";
        }

        private void ClearForm()
        {
            _selectedId = null;
            Grid.SelectedItem = null;

            CodeBox.Text = "";
            NameBox.Text = "";
            DescBox.Text = "";
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