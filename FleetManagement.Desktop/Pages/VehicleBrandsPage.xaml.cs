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
	public partial class VehicleBrandsPage : Page
	{
		private readonly AppDbContext _db = new(App.DbOptions);

		private int? _selectedId;
		private List<VehicleBrand> _all = new();

		public VehicleBrandsPage()
		{
			InitializeComponent();
			Loaded += async (_, __) => await LoadAsync();
		}

		private async Task LoadAsync()
		{
			try
			{
				var list = await _db.VehicleBrands
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
				Notify("Hata: markalar yüklenemedi.", "Hata");
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
				var desc = (DescBox.Text ?? "").Trim();

				if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
				{
					Notify("Kod ve Ad zorunludur.", "Uyarı");
					return;
				}

				// Unique Code kontrolü (soft delete hariç)
				var exists = await _db.VehicleBrands.AsNoTracking()
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
					var entity = new VehicleBrand
					{
						Code = code,
						Name = name,
						Description = string.IsNullOrWhiteSpace(desc) ? null : desc,
						CreatedAt = DateTime.UtcNow,
						IsDeleted = false
					};

					_db.VehicleBrands.Add(entity);
					await _db.SaveChangesAsync();

					Notify($"Kaydedildi: #{entity.Id}");
				}
				else
				{
					var entity = await _db.VehicleBrands.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
					if (entity is null)
					{
						Notify("Kayıt bulunamadı (yenileyin).", "Uyarı");
						return;
					}

					entity.Code = code;
					entity.Name = name;
					entity.Description = string.IsNullOrWhiteSpace(desc) ? null : desc;

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

				var confirm = MessageBox.Show("Seçili marka silinsin mi?", "Onay", MessageBoxButton.YesNo);
				if (confirm != MessageBoxResult.Yes)
					return;

				var entity = await _db.VehicleBrands.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
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
			if (Grid.SelectedItem is not VehicleBrand x)
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
				Grid.ItemsSource = _all;
				FilterInfo.Text = $"Toplam kayıt: {total}";
				return;
			}

			var filtered = _all
				.Where(x =>
					(x.Code ?? "").ToLowerInvariant().Contains(q) ||
					(x.Name ?? "").ToLowerInvariant().Contains(q) ||
					(x.Description ?? "").ToLowerInvariant().Contains(q))
				.ToList();

			Grid.ItemsSource = filtered;
			FilterInfo.Text = $"Toplam kayıt: {filtered.Count} / {total}";
		}

		private void ClearForm()
		{
			_selectedId = null;
			Grid.SelectedItem = null;

			CodeBox.Text = "";
			NameBox.Text = "";
			DescBox.Text = "";
			SearchBox.Text = "";
		}

		private static void Notify(string message, string title = "Bilgi")
		{
			MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}
}