using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ConduitAI.Data;

/// <summary>
/// Design-time factory so `dotnet ef` can create migrations without booting the
/// full web host. Uses a local SQLite file matching the runtime default.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=App_Data/conduitai.db")
            .Options;

        return new AppDbContext(options);
    }
}
