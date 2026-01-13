using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KIOSK.Infrastructure.Database.Ef;

public sealed class KioskDbContextFactory : IDesignTimeDbContextFactory<KioskDbContext>
{
    public KioskDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KioskDbContext>();
        var connectionString = DatabaseConfig.DefaultConnectionString;
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        return new KioskDbContext(optionsBuilder.Options);
    }
}
