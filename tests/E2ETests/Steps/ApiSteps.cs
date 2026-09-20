using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TechTalk.SpecFlow;

namespace KnowledgeBase.E2ETests.Steps;

[Binding]
public static class ApiSteps
{
    public static WebApplicationFactory<Program> CurrentFactory { get; private set; } = null!;
    public static HttpClient CurrentClient { get; private set; } = null!;

    public static Guid CurrentWorkspaceId { get; private set; }

    private static string _dbPath = "";
    private static string _storagePath = "";

    [Given(@"the API is running with a clean database")]
    public static void GivenTheApiIsRunningWithCleanDatabase()
    {
        CurrentFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"kb-e2e-{Guid.NewGuid():N}.db");
            _storagePath = Path.Combine(Path.GetTempPath(), $"kb-e2e-uploads-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_storagePath);
            builder.UseSetting("Database:Provider", "Sqlite");
            builder.UseSetting("Database:ConnectionString", $"Data Source={_dbPath}");
            builder.UseSetting("Storage:RootPath", _storagePath);
        });

        CurrentClient = CurrentFactory.CreateClient();
        CurrentClient.BaseAddress.Should().NotBeNull();
    }

    [Given(@"a workspace named ""(.*)""")]
    public static async Task GivenAWorkspaceNamed(string name)
    {
        var response = await CurrentClient.PostAsJsonAsync("/api/workspaces", new { name, description = (string?)null });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CurrentWorkspaceId = await response.Content.ReadFromJsonAsync<Guid>();
    }

    [AfterScenario]
    public static void DisposeFactory()
    {
        CurrentFactory?.Dispose();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
        if (Directory.Exists(_storagePath))
        {
            Directory.Delete(_storagePath, true);
        }
    }
}