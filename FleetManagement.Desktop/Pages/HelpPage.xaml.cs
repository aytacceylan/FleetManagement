using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
    public partial class HelpPage : Page
    {
        private readonly AppDbContext _db = new(App.DbOptions);

        private int? _selectedId;
        private List<HelpNote> _all = new();

        // PDF yolu (Output’a kopyalanmış olacak)
        private static string GuidePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "UserGuide.pdf");

        public HelpPage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var list = await _db.HelpNotes
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                _all = list;
                Grid.ItemsSource = _all;
                UpdateCount(_all.Count);
            }
            catch (Exception ex)
            {
                Notify("Hata: yardım notları yüklenemedi.", "Hata");
                MessageBox.Show(ex.Message, "Hata");
            }
        }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Grid.SelectedItem is not HelpNote x) return;

            _selectedId = x.Id;
            TitleBox.Text = x.Title ?? "";
            ContentBox.Text = x.Content ?? "";
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            Notify("Temizlendi");
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var title = (TitleBox.Text ?? "").Trim();
                var content = EmptyToNull(ContentBox.Text);

                if (string.IsNullOrWhiteSpace(title))
                {
                    Notify("Başlık zorunlu.");
                    return;
                }

                if (_selectedId is null)
                {
                    var entity = new HelpNote
                    {
                        Title = title,
                        Content = content,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    _db.HelpNotes.Add(entity);
                    await _db.SaveChangesAsync();
                    Notify($"Kaydedildi: #{entity.Id}");
                }
                else
                {
                    var entity = await _db.HelpNotes.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                    if (entity is null)
                    {
                        Notify("Kayıt bulunamadı.");
                        return;
                    }

                    entity.Title = title;
                    entity.Content = content;

                    await _db.SaveChangesAsync();
                    Notify($"Güncellendi: #{entity.Id}");
                }

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
                    Notify("Silmek için listeden kayıt seç.");
                    return;
                }

                var confirm = MessageBox.Show("Seçili kayıt silinsin mi?", "Onay", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes) return;

                var entity = await _db.HelpNotes.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                if (entity is null)
                {
                    Notify("Kayıt bulunamadı.");
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

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = (SearchBox.Text ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(q))
            {
                Grid.ItemsSource = _all;
                UpdateCount(_all.Count);
                return;
            }

            var filtered = _all
                .Where(x =>
                    (x.Title ?? "").ToLowerInvariant().Contains(q) ||
                    (x.Content ?? "").ToLowerInvariant().Contains(q))
                .ToList();

            Grid.ItemsSource = filtered;
            UpdateCount(filtered.Count, _all.Count);
        }

        private void UpdateCount(int shown, int? total = null)
        {
            if (total is null)
                FilterInfo.Text = $"Toplam kayıt: {shown}";
            else
                FilterInfo.Text = $"Filtre: {shown} / {total}";
        }

        private void ClearForm()
        {
            _selectedId = null;
            Grid.SelectedItem = null;

            TitleBox.Text = "";
            ContentBox.Text = "";
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

        // ==========================
        // PDF ACTIONS
        // ==========================
        private void OpenGuide_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(GuidePath))
                {
                    Notify("Kılavuz dosyası bulunamadı. (Assets/UserGuide.pdf)", "Uyarı");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = GuidePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Notify("Kılavuz açılamadı.", "Hata");
                MessageBox.Show(ex.Message, "Hata");
            }
        }

        private void ExportGuide_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(GuidePath))
                {
                    Notify("Kılavuz dosyası bulunamadı. (Assets/UserGuide.pdf)", "Uyarı");
                    return;
                }

                var dlg = new SaveFileDialog
                {
                    Title = "Kılavuzu Dışa Aktar",
                    Filter = "PDF (*.pdf)|*.pdf",
                    FileName = "OtoSevk_KullanimKilavuzu.pdf"
                };

                if (dlg.ShowDialog() != true) return;

                File.Copy(GuidePath, dlg.FileName, overwrite: true);
                Notify("Kılavuz dışa aktarıldı.");
            }
            catch (Exception ex)
            {
                Notify("Dışa aktarma başarısız.", "Hata");
                MessageBox.Show(ex.Message, "Hata");
            }
        }
    }
}