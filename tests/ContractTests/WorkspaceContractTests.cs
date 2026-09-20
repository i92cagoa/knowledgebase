using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using KnowledgeBase.ContractTests;
using Xunit;

namespace KnowledgeBase.ContractTests.Features;

public sealed class WorkspaceContractTests : IClassFixture<ContractTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public WorkspaceContractTests(ContractTestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Crud_Workspace_Lifecycle()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/workspaces", new { name = "Architecture", description = (string?)"notes" });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();

        var list = await (await _client.GetAsync("/api/workspaces")).Content.ReadFromJsonAsync<List<WorkspaceBody>>();
        list!.Should().ContainSingle(w => w.Id == id);

        var details = await _client.GetAsync($"/api/workspaces/{id}");
        details.StatusCode.Should().Be(HttpStatusCode.OK);
        (await details.Content.ReadFromJsonAsync<WorkspaceBody>())!.Name.Should().Be("Architecture");

        var updateResponse = await _client.PutAsJsonAsync($"/api/workspaces/{id}", new { name = "Renamed", description = (string?)null });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deleteResponse = await _client.DeleteAsync($"/api/workspaces/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var missing = await _client.GetAsync($"/api/workspaces/{id}");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Duplicate_Workspace_Returns_Conflict()
    {
        await _client.PostAsJsonAsync("/api/workspaces", new { name = "Unique", description = (string?)null });
        var second = await _client.PostAsJsonAsync("/api/workspaces", new { name = "Unique", description = (string?)null });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    public sealed record WorkspaceBody(Guid Id, string Name, string? Description, DateTime CreatedAt, DateTime UpdatedAt);
}