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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Soft delete filter (şimdilik sadece filtre; DB kolonlarını sonra ekleyeceğiz)
            modelBuilder.Entity<Vehicle>(e =>
            {
                // Plate (envanter no) zaten unique index var demiştin, kalsın.
                e.Property(x => x.Plate).HasMaxLength(50).IsRequired();

                e.Property(x => x.InventoryNumber).HasMaxLength(50);
                e.Property(x => x.Brand).HasMaxLength(80);
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
        }
    }
}