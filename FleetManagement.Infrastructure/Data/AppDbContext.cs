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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Soft delete filter (şimdilik sadece filtre; DB kolonlarını sonra ekleyeceğiz)
            modelBuilder.Entity<Vehicle>().HasQueryFilter(x => !x.IsDeleted);
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
        }
    }
}