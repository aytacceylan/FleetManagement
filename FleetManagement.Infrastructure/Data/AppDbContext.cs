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

            // Soft delete filter (şimdilik sadece filtre; DB kolonlarını sonra ekleyeceğiz)
            modelBuilder.Entity<Vehicle>(e =>
            {
                // Plate (envanter no) zaten unique index var demiştin, kalsın.
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

                // Gerçek plaka tekil olsun istiyorsan:
                e.HasIndex(x => x.InventoryNumber).IsUnique();

                e.HasOne(v => v.AssignedDriver).WithMany().HasForeignKey(v => v.AssignedDriverId).OnDelete(DeleteBehavior.Restrict);
				e.Property(x => x.VehicleYear);



			});
            modelBuilder.Entity<Vehicle>().HasIndex(x => x.AssignedDriverId);

            modelBuilder.Entity<Driver>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<VehicleCommander>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<VehicleMovement>().HasQueryFilter(x => !x.IsDeleted);

            // VehicleMovement ilişkileri
            modelBuilder.Entity<VehicleMovement>(e =>
            {
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

                // daily no id uniq olsun
                e.HasIndex(x => new { x.MovementDate, x.DailyNo }).IsUnique().HasFilter("\"IsDeleted\" = false");


            });

            // Unique indexler (DB'de de var, EF modelinde de dursun)
            modelBuilder.Entity<Driver>()
                .HasIndex(x => x.DriverNumber)
                .IsUnique();


            modelBuilder.Entity<VehicleCommander>()
                .HasIndex(x => x.CommanderNumber)
                .IsUnique();

            modelBuilder.Entity<Vehicle>()
                .HasIndex(x => x.Plate)
                .IsUnique();

            modelBuilder.Entity<Route>(e =>
            {
                e.ToTable("Routes");

                e.Property(x => x.Code).HasMaxLength(50).IsRequired();
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();

                e.Property(x => x.StartPoint).HasMaxLength(200);
                e.Property(x => x.EndPoint).HasMaxLength(200);
                e.Property(x => x.Description).HasMaxLength(500);

                e.HasIndex(x => x.Code).IsUnique(); // kod tekrar etmesin (öneri)
            });

            modelBuilder.Entity<VehicleType>(e =>
            {
                e.ToTable("VehicleTypes");

                e.Property(x => x.Code).HasMaxLength(50).IsRequired();
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.Description).HasMaxLength(500);

                e.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
			});

            modelBuilder.Entity<VehicleCategory>(e =>
            {
                e.ToTable("VehicleCategories");

                e.Property(x => x.Code).HasMaxLength(50).IsRequired();
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.Description).HasMaxLength(500);

                e.HasIndex(x => x.Code).IsUnique();
            });

            modelBuilder.Entity<Departure>(e =>
            {
                e.ToTable("Departures");

                e.Property(x => x.Code).HasMaxLength(50).IsRequired();
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.Description).HasMaxLength(500);

                e.HasIndex(x => x.Code).IsUnique();
            });

			modelBuilder.Entity<VehicleModel>(e =>
			{
				e.ToTable("VehicleModels");

				e.Property(x => x.Code).HasMaxLength(50).IsRequired();
				e.Property(x => x.Name).HasMaxLength(200).IsRequired();
				e.Property(x => x.Description).HasMaxLength(500);

				e.HasIndex(x => x.Code).IsUnique();
			});

			modelBuilder.Entity<Unit>(e =>
			{
				e.ToTable("Units");

				e.Property(x => x.Code).HasMaxLength(50).IsRequired();
				e.Property(x => x.Name).HasMaxLength(200).IsRequired();
				e.Property(x => x.ParentName).HasMaxLength(200);
				e.Property(x => x.Description).HasMaxLength(500);

				e.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
			});

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

                // GuardNumber boş geçilebilir ama doluysa unique olsun
                e.HasIndex(x => x.GuardNumber)
                    .IsUnique()
                    .HasFilter("\"GuardNumber\" IS NOT NULL");
            });

            modelBuilder.Entity<VehicleBrand>(e =>
            {
                e.ToTable("VehicleBrands");
                e.Property(x => x.Code).HasMaxLength(50).IsRequired();
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.Description).HasMaxLength(500);
                e.HasIndex(x => x.Code).IsUnique();
            });
            modelBuilder.Entity<VehicleBrand>().HasQueryFilter(x => !x.IsDeleted);

			modelBuilder.Entity<VehicleYear>(e =>
			{
				e.ToTable("VehicleYears");

				e.Property(x => x.Year).IsRequired();

				e.HasIndex(x => x.Year).IsUnique();
			});

			modelBuilder.Entity<VehicleYear>().HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<DutyType>(e =>
            {
                e.ToTable("DutyTypes");

                e.Property(x => x.Code).HasMaxLength(50);
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.Description).HasMaxLength(200);

                // ✅ Name: sadece aktif kayıtlarda uniq
                e.HasIndex(x => x.Name)
                 .IsUnique()
                 .HasFilter("\"IsDeleted\" = false");

                // ✅ Code: sadece aktif + Code doluysa uniq
                e.HasIndex(x => x.Code)
                 .IsUnique()
                 .HasFilter("\"IsDeleted\" = false AND \"Code\" IS NOT NULL");
            });

            modelBuilder.Entity<HelpNote>(e =>
            {
                e.ToTable("HelpNotes");

                e.Property(x => x.Title).HasMaxLength(200).IsRequired();
                e.Property(x => x.Content).HasMaxLength(2000);

                e.HasIndex(x => x.Title); // unique yapmıyorum; kullanıcı aynı başlığı tekrar yazabilir

                // modelBuilder.Entity<HelpNote>().HasQueryFilter(x => !x.IsDeleted); Soft delete filter istersen:
            });

			modelBuilder.Entity<VehicleType>().HasQueryFilter(x => !x.IsDeleted);
			modelBuilder.Entity<Unit>().HasQueryFilter(x => !x.IsDeleted);
		}
    }
}