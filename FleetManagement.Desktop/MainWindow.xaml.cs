using System;
using System.Windows;
using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Desktop
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseNpgsql("Host=localhost;Port=5432;Database=FleetDb;Username=postgres;Password=1234")
				.Options;

			using var db = new AppDbContext(options);

			var vehicle = new Vehicle
			{
				Plate = "34FAST001",
				Brand = "Toyota",
				Model = "Corolla",
				CreatedAt = DateTime.UtcNow
			};

			db.Vehicles.Add(vehicle);
			await db.SaveChangesAsync();

			MessageBox.Show("Kayıt eklendi. ID: " + vehicle.Id);
		}
	}
}

