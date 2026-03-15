using FleetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Infrastructure.Data
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options)
			: base(options) { }

		public DbSet<Vehicle> Vehicles => Set<Vehicle>();
		public DbSet<Driver> Drivers => Set<Driver>();
		public DbSet<VehicleCommander> VehicleCommanders => Set<VehicleCommander>();
		public DbSet<VehicleMovement> VehicleMovements => Set<VehicleMovement>();
		public DbSet<Route> Routes => Set<Route>();
		public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
		public DbSet<VehicleCategory> VehicleCategories => Set<VehicleCategory>();
		public DbSet<Departure> Departures => Set<Departure>();
		public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
		public DbSet<Unit> Units => Set<Unit>();
		public DbSet<VehicleGuard> VehicleGuards => Set<VehicleGuard>();
		public DbSet<VehicleBrand> VehicleBrands => Set<VehicleBrand>();
		public DbSet<VehicleYear> VehicleYears => Set<VehicleYear>();
		public DbSet<DutyType> DutyTypes => Set<DutyType>();
		public DbSet<HelpNote> HelpNotes => Set<HelpNote>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// =========================
			// VEHICLE
			// =========================
			modelBuilder.Entity<Vehicle>(e =>
			{
				e.Property(x => x.Plate).HasMaxLength(50).IsRequired();
				e.Property(x => x.InventoryNumber).HasMaxLength(50);

				e.Property(x => x.VehicleBrand).HasMaxLength(80);
				e.Property(x => x.Model).HasMaxLength(80);
				e.Property(x => x.VehicleType).HasMaxLength(80);
				e.Property(x => x.VehicleCategory).HasMaxLength(80);
				e.Property(x => x.VehicleUnit).HasMaxLength(120);

				e.Property(x => x.MotorNo).HasMaxLength(80);
				e.Property(x => x.SaseNo).HasMaxLength(80);
				e.Property(x => x.VehicleSituation).HasMaxLength(80);

				e.Property(x => x.VehicleYear);

				// sadece aktif araçlarda plaka unique olsun
				e.HasIndex(x => x.Plate)
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false");

				// envanter no doluysa ve aktifse unique olsun
				e.HasIndex(x => x.InventoryNumber)
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false AND \"InventoryNumber\" IS NOT NULL");

				e.HasOne(v => v.AssignedDriver)
					.WithMany()
					.HasForeignKey(v => v.AssignedDriverId)
					.OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<Vehicle>()
				.HasIndex(x => x.AssignedDriverId);

			// =========================
			// DRIVER
			// =========================
			modelBuilder.Entity<Driver>(e =>
			{
				e.HasQueryFilter(x => !x.IsDeleted);

				e.HasIndex(x => x.DriverNumber)
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false");
			});

			// =========================
			// VEHICLE COMMANDER
			// =========================
			modelBuilder.Entity<VehicleCommander>(e =>
			{
				e.HasQueryFilter(x => !x.IsDeleted);

				e.HasIndex(x => x.CommanderNumber)
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false");
			});

			// =========================
			// VEHICLE MOVEMENT
			// =========================
			modelBuilder.Entity<VehicleMovement>(e =>
			{
				e.HasQueryFilter(x => !x.IsDeleted);

				e.HasOne(x => x.Vehicle)
					.WithMany()
					.HasForeignKey(x => x.VehicleId)
					.OnDelete(DeleteBehavior.Restrict);

				e.HasOne(x => x.Driver)
					.WithMany()
					.HasForeignKey(x => x.DriverId)
					.OnDelete(DeleteBehavior.Restrict);

				e.HasOne(x => x.VehicleCommander)
					.WithMany()
					.HasForeignKey(x => x.VehicleCommanderId)
					.OnDelete(DeleteBehavior.Restrict);

				e.HasOne(x => x.SecondDriver)
					.WithMany()
					.HasForeignKey(x => x.SecondDriverId)
					.OnDelete(DeleteBehavior.Restrict);

				// Aynı tarihte aynı günlük no, sadece aktif kayıtlarda tekil olsun
				e.HasIndex(x => new { x.MovementDate, x.DailyNo })
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false");
			});

			// =========================
			// ROUTE
			// =========================
			modelBuilder.Entity<Route>(e =>
			{
				e.ToTable("Routes");

				e.Property(x => x.Code).HasMaxLength(50).IsRequired();
				e.Property(x => x.Name).HasMaxLength(200).IsRequired();
				e.Property(x => x.StartPoint).HasMaxLength(200);
				e.Property(x => x.EndPoint).HasMaxLength(200);
				e.Property(x => x.Description).HasMaxLength(500);

				e.HasQueryFilter(x => !x.IsDeleted);

				e.HasIndex(x => x.Code)
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false");
			});

			// =========================
			// VEHICLE TYPE
			// =========================
			modelBuilder.Entity<VehicleType>(e =>
			{
				e.ToTable("VehicleTypes");

				e.Property(x => x.Code).HasMaxLength(50).IsRequired();
				e.Property(x => x.Name).HasMaxLength(200).IsRequired();
				e.Property(x => x.Description).HasMaxLength(500);

				e.HasQueryFilter(x => !x.IsDeleted);

				e.HasIndex(x => x.Code)
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false");
			});

			// =========================
			// VEHICLE CATEGORY
			// =========================
			modelBuilder.Entity<VehicleCategory>(e =>
			{
				e.ToTable("VehicleCategories");

				e.Property(x => x.Code).HasMaxLength(50).IsRequired();
				e.Property(x => x.Name).HasMaxLength(200).IsRequired();
				e.Property(x => x.Description).HasMaxLength(500);

				e.HasQueryFilter(x => !x.IsDeleted);

				e.HasIndex(x => x.Code)
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false");
			});

			// =========================
			// DEPARTURE
			// =========================
			modelBuilder.Entity<Departure>(e =>
			{
				e.ToTable("Departures");

				e.Property(x => x.Code).HasMaxLength(50).IsRequired();
				e.Property(x => x.Name).HasMaxLength(200).IsRequired();
				e.Property(x => x.Description).HasMaxLength(500);

				e.HasQueryFilter(x => !x.IsDeleted);

				e.HasIndex(x => x.Code)
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false");
			});

			// =========================
			// VEHICLE MODEL
			// =========================
			modelBuilder.Entity<VehicleModel>(e =>
			{
				e.ToTable("VehicleModels");

				e.Property(x => x.Code).HasMaxLength(50).IsRequired();
				e.Property(x => x.Name).HasMaxLength(200).IsRequired();
				e.Property(x => x.Description).HasMaxLength(500);

				e.HasQueryFilter(x => !x.IsDeleted);

				e.HasIndex(x => x.Code)
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false");
			});

			// =========================
			// UNIT
			// =========================
			modelBuilder.Entity<Unit>(e =>
			{
				e.ToTable("Units");

				e.Property(x => x.Code).HasMaxLength(50).IsRequired();
				e.Property(x => x.Name).HasMaxLength(200).IsRequired();
				e.Property(x => x.ParentName).HasMaxLength(200);
				e.Property(x => x.Description).HasMaxLength(500);

				e.HasQueryFilter(x => !x.IsDeleted);

				e.HasIndex(x => x.Code)
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false");
			});

			// =========================
			// VEHICLE GUARD
			// =========================
			modelBuilder.Entity<VehicleGuard>(e =>
			{
				e.ToTable("VehicleGuards");

				e.Property(x => x.GuardNumber)
					.HasMaxLength(50)
					.IsRequired(false);

				e.Property(x => x.FullName)
					.HasMaxLength(200)
					.IsRequired();

				e.Property(x => x.PhoneNumber)
					.HasMaxLength(50)
					.IsRequired(false);

				// GuardNumber boş olabilir; doluysa tekil olsun
				e.HasIndex(x => x.GuardNumber)
					.IsUnique()
					.HasFilter("\"GuardNumber\" IS NOT NULL");
			});

			// =========================
			// VEHICLE BRAND
			// =========================
			modelBuilder.Entity<VehicleBrand>(e =>
			{
				e.ToTable("VehicleBrands");

				e.Property(x => x.Code).HasMaxLength(50).IsRequired();
				e.Property(x => x.Name).HasMaxLength(200).IsRequired();
				e.Property(x => x.Description).HasMaxLength(500);

				e.HasQueryFilter(x => !x.IsDeleted);

				e.HasIndex(x => x.Code)
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false");
			});

			// =========================
			// VEHICLE YEAR
			// =========================
			modelBuilder.Entity<VehicleYear>(e =>
			{
				e.ToTable("VehicleYears");

				e.Property(x => x.Year).IsRequired();

				e.HasQueryFilter(x => !x.IsDeleted);

				e.HasIndex(x => x.Year)
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false");
			});

			// =========================
			// DUTY TYPE
			// =========================
			modelBuilder.Entity<DutyType>(e =>
			{
				e.ToTable("DutyTypes");

				e.Property(x => x.Code).HasMaxLength(50);
				e.Property(x => x.Name).HasMaxLength(200).IsRequired();
				e.Property(x => x.Description).HasMaxLength(200);

				e.HasQueryFilter(x => !x.IsDeleted);

				// Name: sadece aktif kayıtlarda uniq
				e.HasIndex(x => x.Name)
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false");

				// Code: sadece aktif kayıtlarda ve doluysa uniq
				e.HasIndex(x => x.Code)
					.IsUnique()
					.HasFilter("\"IsDeleted\" = false AND \"Code\" IS NOT NULL");
			});

			// =========================
			// HELP NOTE
			// =========================
			modelBuilder.Entity<HelpNote>(e =>
			{
				e.ToTable("HelpNotes");

				e.Property(x => x.Title).HasMaxLength(200).IsRequired();
				e.Property(x => x.Content).HasMaxLength(2000);

				e.HasIndex(x => x.Title);
				// Soft delete kullanılacaksa buraya query filter eklenebilir
			});
		}
	}
}