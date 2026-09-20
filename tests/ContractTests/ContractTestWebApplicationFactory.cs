using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KnowledgeBase.ContractTests;

public sealed class ContractTestWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DbPath { get; } = Path.Combine(Path.GetTempPath(), $"kb-contract-{Guid.NewGuid():N}.db");
    public string StoragePath { get; } = Path.Combine(Path.GetTempPath(), $"kb-contract-uploads-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(StoragePath);
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Database:ConnectionString", $"Data Source={DbPath}");
        builder.UseSetting("Storage:RootPath", StoragePath);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(DbPath))
        {
            File.Delete(DbPath);
        }
        if (Directory.Exists(StoragePath))
        {
            Directory.Delete(StoragePath, true);
        }
    }
}