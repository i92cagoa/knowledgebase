using KnowledgeBase.Application;
using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.Features.Links;
using KnowledgeBase.Infrastructure.Links;
using KnowledgeBase.Infrastructure.Persistence;
using KnowledgeBase.Infrastructure.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KnowledgeBase.UnitTests.Fixtures;

public sealed class TestAppDbContext : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public TestAppDbContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        StorageRoot = Path.Combine(Path.GetTempPath(), $"kb-uploads-{Guid.NewGuid():N}");
        Directory.CreateDirectory(StorageRoot);

        var services = new ServiceCollection();
        services.AddScoped(_ => Options.Create(new AttachmentStorageOptions { RootPath = StorageRoot }));
        services.AddScoped<IAttachmentStorage, DiskAttachmentStorage>();
        services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(_connection));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddApplication();
        services.AddScoped<ILinkContentFetcher, FakeLinkContentFetcher>();
        services.AddScoped<ILinkAnalyzer, RulesLinkAnalyzer>();

        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    public string StorageRoot { get; }

    public IServiceProvider Provider => _provider;

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();

        if (Directory.Exists(StorageRoot))
        {
            Directory.Delete(StorageRoot, true);
        }
    }
}

public sealed class FakeLinkContentFetcher : ILinkContentFetcher
{
    public Task<FetchedLink> FetchAsync(Uri url, CancellationToken cancellationToken) =>
        Task.FromResult(new FetchedLink(
            "Example article",
            "Postgres caching is essential for production databases. Caching strategies improve latency. Postgres developers rely on caching every day.",
            "Postgres caching is essential for production databases."));
}