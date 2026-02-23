using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Views
{
	public partial class DriverView : UserControl
	{
		private readonly DbContextOptions<AppDbContext> _dbOptions =
			new DbContextOptionsBuilder<AppDbContext>()
				.UseNpgsql("Host=127.0.0.1;Port=5432;Database=FleetDb;Username=postgres;Password=1234")
				.Options;

		public DriverView()
		{
			InitializeComponent();
			Loaded += DriverView_Loaded;
		}

		private async void DriverView_Loaded(object sender, RoutedEventArgs e)
		{
			await LoadDriversAsync();
		}

		private async Task LoadDriversAsync()
		{
			using var db = new AppDbContext(_dbOptions);

			var list = await db.Set<Driver>()
				.AsNoTracking()
				.Where(x => !x.IsDeleted)
				.OrderByDescending(x => x.Id)
				.ToListAsync();

			DriversGrid.ItemsSource = list;
		}

		private async void Refresh_Click(object sender, RoutedEventArgs e)
		{
			await LoadDriversAsync();
		}

		private async void Save_Click(object sender, RoutedEventArgs e)
		{
			var fullName = FullNameBox.Text?.Trim();
			var phone = PhoneBox.Text?.Trim();

			if (string.IsNullOrWhiteSpace(fullName))
			{
				MessageBox.Show("Ad Soyad zorunlu.");
				return;
			}

			if (string.IsNullOrWhiteSpace(phone))
			{
				MessageBox.Show("Telefon zorunlu.");
				return;
			}

			using var db = new AppDbContext(_dbOptions);

			var driver = new Driver
			{
				FullName = fullName,
				PhoneNumber = phone,       // <- doğru alan
				CreatedAt = DateTime.UtcNow,
				IsDeleted = false
			};

			db.Set<Driver>().Add(driver);
			await db.SaveChangesAsync();

			FullNameBox.Text = "";
			PhoneBox.Text = "";

			await LoadDriversAsync();
		}
	}
}