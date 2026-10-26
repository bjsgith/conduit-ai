using Microsoft.Data.Sqlite;

namespace ConduitAI.Data;

/// <summary>Resolves the public default database path independently of the process working directory.</summary>
public static class DatabasePaths
{
    public const string DefaultRelativePath = "App_Data/conduitai.db";

    public static string ResolveConnectionString(string? configuredConnectionString, string contentRootPath)
    {
        var connectionString = string.IsNullOrWhiteSpace(configuredConnectionString)
            ? $"Data Source={DefaultRelativePath}"
            : configuredConnectionString;

        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.Equals(builder.DataSource, DefaultRelativePath, StringComparison.OrdinalIgnoreCase))
        {
            builder.DataSource = Path.GetFullPath(DefaultRelativePath, contentRootPath);
        }

        return builder.ToString();
    }
}
