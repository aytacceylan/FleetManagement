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
				FormInfo.Text = "Yükleniyor...";

				var list = await _db.Units
					.AsNoTracking()
					.Where(x => !x.IsDeleted)
					.OrderByDescending(x => x.Id)
					.ToListAsync();

				_all = list;
				Grid.ItemsSource = _all;

				FilterInfo.Text = $"Toplam kayıt: {_all.Count}";
				FormInfo.Text = $"Yüklendi: {_all.Count} kayıt";
			}
			catch (Exception ex)
			{
				FormInfo.Text = "Hata: birlikler yüklenemedi.";
				MessageBox.Show(ex.Message, "Hata");
			}
		}

		private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Grid.SelectedItem is not Unit x)
				return;

			_selectedId = x.Id;

			CodeBox.Text = x.Code ?? "";
			NameBox.Text = x.Name ?? "";
			ParentBox.Text = x.ParentName ?? "";
			DescBox.Text = x.Description ?? "";

			FormInfo.Text = $"Seçildi: #{x.Id}";
		}

		private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

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
				var parent = (ParentBox.Text ?? "").Trim();
				var desc = (DescBox.Text ?? "").Trim();

				if (string.IsNullOrWhiteSpace(code))
				{
					FormInfo.Text = "Birlik Kodu zorunlu.";
					return;
				}

				if (string.IsNullOrWhiteSpace(name))
				{
					FormInfo.Text = "Birlik Adı zorunlu.";
					return;
				}

				if (_selectedId is null)
				{
					var entity = new Unit
					{
						Code = code,
						Name = name,
						ParentName = string.IsNullOrWhiteSpace(parent) ? null : parent,
						Description = string.IsNullOrWhiteSpace(desc) ? null : desc,
						CreatedAt = DateTime.UtcNow,
						IsDeleted = false
					};

					_db.Units.Add(entity);
					await _db.SaveChangesAsync();
					FormInfo.Text = $"Kaydedildi: #{entity.Id}";
				}
				else
				{
					var entity = await _db.Units.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
					if (entity is null)
					{
						FormInfo.Text = "Kayıt bulunamadı (yenileyin).";
						return;
					}

					entity.Code = code;
					entity.Name = name;
					entity.ParentName = string.IsNullOrWhiteSpace(parent) ? null : parent;
					entity.Description = string.IsNullOrWhiteSpace(desc) ? null : desc;

					await _db.SaveChangesAsync();
					FormInfo.Text = $"Güncellendi: #{entity.Id}";
				}

				await LoadAsync();
				ClearForm();
			}
			catch (DbUpdateException dbex)
			{
				FormInfo.Text = "Hata: kayıt yapılamadı (muhtemelen Birlik Kodu tekrar ediyor).";
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

				var confirm = MessageBox.Show("Seçili birlik silinsin mi?", "Onay", MessageBoxButton.YesNo);
				if (confirm != MessageBoxResult.Yes)
					return;

				var entity = await _db.Units.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
				if (entity is null)
				{
					FormInfo.Text = "Kayıt bulunamadı (yenileyin).";
					return;
				}

				entity.IsDeleted = true;
				await _db.SaveChangesAsync();

				FormInfo.Text = $"Silindi: #{_selectedId.Value}";

				await LoadAsync();
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
			FilterInfo.Text = $"Filtre: \"{q}\" → {filtered.Count} / {total} kayıt";
		}

		private void ClearForm()
		{
			_selectedId = null;
			Grid.SelectedItem = null;

			CodeBox.Text = "";
			NameBox.Text = "";
			ParentBox.Text = "";
			DescBox.Text = "";

			SearchBox.Text = ""; // ✅ filtreyi sıfırla
		}
	}
}