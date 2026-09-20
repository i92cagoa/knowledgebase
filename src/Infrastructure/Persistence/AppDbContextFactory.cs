using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KnowledgeBase.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var providerName = Environment.GetEnvironmentVariable("KB_DATABASE_PROVIDER") ?? "sqlite";
        var connectionString = Environment.GetEnvironmentVariable("KB_CONNECTION_STRING")
            ?? DatabaseOptionsDefaults.SqliteConnectionString;

        var provider = providerName.Equals("postgres", StringComparison.OrdinalIgnoreCase)
            ? DatabaseProvider.Postgres
            : DatabaseProvider.Sqlite;

        var options = new DatabaseOptions
        {
            Provider = provider,
            ConnectionString = connectionString
        };

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseDatabaseProvider(options);

        return new AppDbContext(builder.Options);
    }
}