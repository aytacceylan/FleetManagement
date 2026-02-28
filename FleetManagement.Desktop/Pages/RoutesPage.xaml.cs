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
    public partial class RoutesPage : Page
    {
        private readonly AppDbContext _db = new(App.DbOptions);

        private int? _selectedId;
        private List<Route> _allRoutes = new();

        public RoutesPage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadRoutesAsync();
        }

        private async Task LoadRoutesAsync()
        {
            try
            {
                FormInfo.Text = "Yükleniyor...";

                var list = await _db.Routes
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)          // ✅ soft delete filtresi
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                _allRoutes = list;
                RoutesGrid.ItemsSource = _allRoutes;

                FormInfo.Text = $"Yüklendi: {_allRoutes.Count} kayıt";
            }
            catch (Exception ex)
            {
                FormInfo.Text = "Hata: rotalar yüklenemedi.";
                MessageBox.Show(ex.Message, "Hata");
            }
        }

        private void RoutesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoutesGrid.SelectedItem is not Route r)
                return;

            _selectedId = r.Id;

            CodeBox.Text = r.Code ?? "";
            NameBox.Text = r.Name ?? "";
            StartBox.Text = r.StartPoint ?? "";
            EndBox.Text = r.EndPoint ?? "";
            DescBox.Text = r.Description ?? "";

            FormInfo.Text = $"Seçildi: #{r.Id}";
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadRoutesAsync();
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
                var code = (CodeBox.Text ?? "").Trim();
                var name = (NameBox.Text ?? "").Trim();

                var start = (StartBox.Text ?? "").Trim();
                var end = (EndBox.Text ?? "").Trim();
                var desc = (DescBox.Text ?? "").Trim();

                if (string.IsNullOrWhiteSpace(code))
                {
                    FormInfo.Text = "Rota Kodu zorunlu.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    FormInfo.Text = "Rota Adı zorunlu.";
                    return;
                }

                if (_selectedId is null)
                {
                    // INSERT
                    var entity = new Route
                    {
                        Code = code,
                        Name = name,
                        StartPoint = string.IsNullOrWhiteSpace(start) ? null : start,
                        EndPoint = string.IsNullOrWhiteSpace(end) ? null : end,
                        Description = string.IsNullOrWhiteSpace(desc) ? null : desc,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    _db.Routes.Add(entity);
                    await _db.SaveChangesAsync();

                    FormInfo.Text = $"Kaydedildi: #{entity.Id}";
                }
                else
                {
                    // UPDATE
                    var entity = await _db.Routes.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                    if (entity is null)
                    {
                        FormInfo.Text = "Kayıt bulunamadı (yenileyin).";
                        return;
                    }

                    entity.Code = code;
                    entity.Name = name;
                    entity.StartPoint = string.IsNullOrWhiteSpace(start) ? null : start;
                    entity.EndPoint = string.IsNullOrWhiteSpace(end) ? null : end;
                    entity.Description = string.IsNullOrWhiteSpace(desc) ? null : desc;

                    await _db.SaveChangesAsync();

                    FormInfo.Text = $"Güncellendi: #{entity.Id}";
                }

                await LoadRoutesAsync();
                ClearForm();
            }
            catch (DbUpdateException dbex)
            {
                // Code unique index -> aynı kod girilirse buraya düşer
                FormInfo.Text = "Hata: kayıt yapılamadı (muhtemelen Rota Kodu tekrar ediyor).";
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

                var confirm = MessageBox.Show("Seçili rota silinsin mi?", "Onay", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes)
                    return;

                var entity = await _db.Routes.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                if (entity is null)
                {
                    FormInfo.Text = "Kayıt bulunamadı (yenileyin).";
                    return;
                }

                // SOFT DELETE
                entity.IsDeleted = true;
                await _db.SaveChangesAsync();

                FormInfo.Text = $"Silindi: #{_selectedId.Value}";

                await LoadRoutesAsync();
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
        }

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var q = (SearchBox.Text ?? "").Trim().ToLowerInvariant();
			var total = _allRoutes.Count;

			if (string.IsNullOrWhiteSpace(q))
			{
				RoutesGrid.ItemsSource = _allRoutes;
				FilterInfo.Text = $"Toplam kayıt: {total}";
				return;
			}

			var filtered = _allRoutes
				.Where(x =>
					(x.Code ?? "").ToLowerInvariant().Contains(q) ||
					(x.Name ?? "").ToLowerInvariant().Contains(q) ||
					(x.StartPoint ?? "").ToLowerInvariant().Contains(q) ||
					(x.EndPoint ?? "").ToLowerInvariant().Contains(q) ||
					(x.Description ?? "").ToLowerInvariant().Contains(q))
				.ToList();

			RoutesGrid.ItemsSource = filtered;
			FilterInfo.Text = $"Filtre: \"{q}\" → {filtered.Count} / {total} kayıt";
		}

		private void ClearForm()
        {
            _selectedId = null;
            RoutesGrid.SelectedItem = null;

            CodeBox.Text = "";
            NameBox.Text = "";
            StartBox.Text = "";
            EndBox.Text = "";
            DescBox.Text = "";
        }
    }
}