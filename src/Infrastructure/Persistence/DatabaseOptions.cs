namespace KnowledgeBase.Infrastructure.Persistence;

public enum DatabaseProvider
{
    Sqlite = 1,
    Postgres = 2
}

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;
    public string ConnectionString { get; set; } = string.Empty;
}

public static class DatabaseOptionsDefaults
{
    public const string SqliteConnectionString = "Data Source=knowledgebase.db";
}