using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kiosk.Infrastructure.Database.Ef;

public sealed class KioskDbContextFactory : IDesignTimeDbContextFactory<KioskDbContext>
{
    public KioskDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KioskDbContext>();
        var connectionString = DatabaseConfig.DefaultConnectionString;
        optionsBuilder.UseSqlite(connectionString);
        return new KioskDbContext(optionsBuilder.Options);
    }
}
