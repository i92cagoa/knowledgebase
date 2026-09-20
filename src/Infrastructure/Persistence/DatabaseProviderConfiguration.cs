using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Persistence;

public static class DatabaseProviderConfiguration
{
    public static DbContextOptionsBuilder UseDatabaseProvider(
        this DbContextOptionsBuilder options,
        DatabaseOptions databaseOptions,
        string? migrationsAssembly = null)
    {
        return databaseOptions.Provider switch
        {
            DatabaseProvider.Postgres => UsePostgresProvider(options, databaseOptions, migrationsAssembly),
            _ => UseSqliteProvider(options, databaseOptions, migrationsAssembly)
        };
    }

    private static DbContextOptionsBuilder UseSqliteProvider(
        DbContextOptionsBuilder options,
        DatabaseOptions databaseOptions,
        string? migrationsAssembly)
    {
        options.UseSqlite(
            databaseOptions.ConnectionString,
            sqlite =>
            {
                if (!string.IsNullOrWhiteSpace(migrationsAssembly))
                {
                    sqlite.MigrationsAssembly(migrationsAssembly);
                }
            });
        return options;
    }

    private static DbContextOptionsBuilder UsePostgresProvider(
        DbContextOptionsBuilder options,
        DatabaseOptions databaseOptions,
        string? migrationsAssembly)
    {
        options.UseNpgsql(
            databaseOptions.ConnectionString,
            npgsql =>
            {
                if (!string.IsNullOrWhiteSpace(migrationsAssembly))
                {
                    npgsql.MigrationsAssembly(migrationsAssembly);
                }
            });
        return options;
    }
}