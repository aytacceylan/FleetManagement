using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FleetManagement.Infrastructure.Data
{
	public class DesignTimeDbContextFactory
		: IDesignTimeDbContextFactory<AppDbContext>
	{
		public AppDbContext CreateDbContext(string[] args)
		{
			var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

			optionsBuilder.UseNpgsql(
				"Host=localhost;Port=5432;Database=FleetDb;Username=postgres;Password=1234"
			);

			return new AppDbContext(optionsBuilder.Options);
		}
	}
}

