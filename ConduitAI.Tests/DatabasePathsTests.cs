using ConduitAI.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace ConduitAI.Tests;

public class DatabasePathsTests
{
    [Fact]
    public void ResolveConnectionString_AnchorsDefaultDatabaseAtContentRoot()
    {
        var resolved = new SqliteConnectionStringBuilder(DatabasePaths.ResolveConnectionString(
            "Data Source=App_Data/conduitai.db", "/tmp/conduitai-content"));

        Assert.Equal("/tmp/conduitai-content/App_Data/conduitai.db", resolved.DataSource);
    }

    [Fact]
    public void ResolveConnectionString_PreservesConfiguredDatabaseOverride()
    {
        const string configured = "Data Source=/var/tmp/custom-conduitai.db;Mode=ReadWriteCreate;Cache=Shared";

        var resolved = new SqliteConnectionStringBuilder(DatabasePaths.ResolveConnectionString(
            configured, "/tmp/conduitai-content"));

        Assert.Equal("/var/tmp/custom-conduitai.db", resolved.DataSource);
        Assert.Equal(SqliteOpenMode.ReadWriteCreate, resolved.Mode);
        Assert.Equal(SqliteCacheMode.Shared, resolved.Cache);
    }
}
