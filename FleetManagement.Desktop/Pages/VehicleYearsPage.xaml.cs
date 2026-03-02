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
	public partial class VehicleYearsPage : Page
	{
		private readonly AppDbContext _db = new(App.DbOptions);

		private int? _selectedId;
		private List<VehicleYear> _all = new();

		public VehicleYearsPage()
		{
			InitializeComponent();
			Loaded += async (_, __) => await LoadAsync();
		}

		private async Task LoadAsync()
		{
			try
			{
				var list = await _db.VehicleYears
					.AsNoTracking()
					.Where(x => !x.IsDeleted)
					.OrderByDescending(x => x.Year)
					.ToListAsync();

				_all = list;
				Grid.ItemsSource = _all;
				FilterInfo.Text = $"Toplam kayıt: {_all.Count}";
			}
			catch (Exception ex)
			{
				Notify("Hata: yıllar yüklenemedi.", "Hata");
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
				var yearText = (YearBox.Text ?? "").Trim();

				if (!int.TryParse(yearText, out var year) || year < 1900 || year > 2100)
				{
					Notify("Geçerli bir yıl gir (1900-2100).", "Uyarı");
					return;
				}

				var exists = await _db.VehicleYears.AsNoTracking()
					.AnyAsync(x => !x.IsDeleted
								   && x.Year == year
								   && (_selectedId == null || x.Id != _selectedId.Value));

				if (exists)
				{
					Notify("Bu yıl zaten var.", "Uyarı");
					return;
				}

				if (_selectedId is null)
				{
					var entity = new VehicleYear
					{
						Year = year,
						CreatedAt = DateTime.UtcNow,
						IsDeleted = false
					};

					_db.VehicleYears.Add(entity);
					await _db.SaveChangesAsync();

					Notify($"Kaydedildi: #{entity.Id}");
				}
				else
				{
					var entity = await _db.VehicleYears.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
					if (entity is null)
					{
						Notify("Kayıt bulunamadı (yenileyin).", "Uyarı");
						return;
					}

					entity.Year = year;
					await _db.SaveChangesAsync();

					Notify($"Güncellendi: #{entity.Id}");
				}

				await LoadAsync();
				ClearForm();
			}
			catch (DbUpdateException dbex)
			{
				Notify("Hata: kayıt yapılamadı (muhtemelen yıl tekrar ediyor).", "DB Hatası");
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

				var confirm = MessageBox.Show("Seçili yıl silinsin mi?", "Onay", MessageBoxButton.YesNo);
				if (confirm != MessageBoxResult.Yes)
					return;

				var entity = await _db.VehicleYears.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
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

		private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Grid.SelectedItem is not VehicleYear x)
				return;

			_selectedId = x.Id;
			YearBox.Text = x.Year.ToString();
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var q = (SearchBox.Text ?? "").Trim();
			var total = _all.Count;

			if (string.IsNullOrWhiteSpace(q))
			{
				Grid.ItemsSource = _all;
				FilterInfo.Text = $"Toplam kayıt: {total}";
				return;
			}

			var filtered = _all
				.Where(x => x.Year.ToString().Contains(q))
				.ToList();

			Grid.ItemsSource = filtered;
			FilterInfo.Text = $"Toplam kayıt: {filtered.Count} / {total}";
		}

		private void ClearForm()
		{
			_selectedId = null;
			Grid.SelectedItem = null;

			YearBox.Text = "";
			SearchBox.Text = "";
		}

		private static void Notify(string message, string title = "Bilgi")
		{
			MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}
}