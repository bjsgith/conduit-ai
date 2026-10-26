using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ConduitAI.Data;

/// <summary>
/// Design-time factory so `dotnet ef` can create migrations without booting the
/// full web host. Uses a local SQLite file matching the runtime default.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var contentRoot = FindContentRoot();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(contentRoot)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = DatabasePaths.ResolveConnectionString(
            configuration.GetConnectionString("DefaultConnection"), contentRoot);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static string FindContentRoot()
    {
        var workingDirectory = Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(workingDirectory, "ConduitAI.csproj")))
        {
            return workingDirectory;
        }

        var projectDirectory = Path.Combine(workingDirectory, "ConduitAI");
        if (File.Exists(Path.Combine(projectDirectory, "ConduitAI.csproj")))
        {
            return projectDirectory;
        }

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ConduitAI.csproj")))
            {
                return directory.FullName;
            }
        }

        return workingDirectory;
    }
}
