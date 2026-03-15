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
	public partial class UnitsPage : Page
	{
		private readonly AppDbContext _db = new(App.DbOptions);

		private int? _selectedId;
		private List<Unit> _all = new();

		public UnitsPage()
		{
			InitializeComponent();
			Loaded += async (_, __) => await LoadAsync();
		}

		private async Task LoadAsync()
		{
			try
			{
				var list = await _db.Units
					.AsNoTracking()
					.OrderByDescending(x => x.Id)
					.ToListAsync();

				_all = list;
				Grid.ItemsSource = _all;
				FilterInfo.Text = $"Toplam kayıt: {_all.Count}";
			}
			catch (Exception ex)
			{
				Notify("Hata: Ait Olduğu Takım yüklenemedi.", "Hata");
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
				var parent = (ParentNameBox.Text ?? "").Trim();
				var desc = (DescBox.Text ?? "").Trim();

				if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
				{
					Notify("Kod ve Ait Olduğu Takım zorunludur.", "Uyarı");
					return;
				}

				var exists = await _db.Units.AsNoTracking()
					.AnyAsync(x =>
						x.Code.ToLower() == code.ToLower() &&
						(_selectedId == null || x.Id != _selectedId.Value));

				if (exists)
				{
					Notify("Bu kod zaten var.", "Uyarı");
					return;
				}

				if (_selectedId is null)
				{
					var entity = new Unit
					{
						Code = code,
						Name = name,
						ParentName = EmptyToNull(parent),
						Description = EmptyToNull(desc),
						CreatedAt = DateTime.UtcNow
					};

					_db.Units.Add(entity);
					await _db.SaveChangesAsync();

					Notify($"Kaydedildi: #{entity.Id}");
				}
				else
				{
					var entity = await _db.Units.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
					if (entity is null)
					{
						Notify("Kayıt bulunamadı (yenileyin).", "Uyarı");
						return;
					}

					entity.Code = code;
					entity.Name = name;
					entity.ParentName = EmptyToNull(parent);
					entity.Description = EmptyToNull(desc);

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

				var confirm = MessageBox.Show("Seçili birlik silinsin mi?", "Onay", MessageBoxButton.YesNo);
				if (confirm != MessageBoxResult.Yes)
					return;

				var entity = await _db.Units.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
				if (entity is null)
				{
					Notify("Kayıt bulunamadı (yenileyin).", "Uyarı");
					return;
				}

				entity.IsDeleted = true;          // ✅ Soft delete
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
			if (Grid.SelectedItem is not Unit x)
				return;

			_selectedId = x.Id;

			CodeBox.Text = x.Code ?? "";
			NameBox.Text = x.Name ?? "";
			ParentNameBox.Text = x.ParentName ?? "";
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
					(x.ParentName ?? "").ToLowerInvariant().Contains(q) ||
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
			ParentNameBox.Text = "";
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