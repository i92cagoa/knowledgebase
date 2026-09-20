using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Xunit;

namespace KnowledgeBase.ContractTests.Features;

public sealed class GraphContractTests : IClassFixture<ContractTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GraphContractTests(ContractTestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateWorkspaceAsync()
    {
        var name = $"Graph-{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/api/workspaces", new { name, description = (string?)null });
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> CreateNoteAsync(Guid workspaceId, string title, string[] tags)
    {
        var response = await _client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/notes", new
        {
            title,
            contentMarkdown = "body",
            tags
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task Get_Graph_Returns_Nodes_And_Edges()
    {
        var workspaceId = await CreateWorkspaceAsync();
        var n1 = await CreateNoteAsync(workspaceId, "One", ["shared"]);
        var n2 = await CreateNoteAsync(workspaceId, "Two", ["shared", "extra"]);
        var n3 = await CreateNoteAsync(workspaceId, "Three", ["unrelated"]);

        var response = await _client.GetAsync("/api/graph");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GraphBody>();
        body!.Nodes.Select(n => n.Id).Should().Contain(n1).And.Contain(n2).And.Contain(n3);
        body.Edges.Should().ContainSingle(e =>
            (e.SourceNoteId == n1 && e.TargetNoteId == n2) ||
            (e.SourceNoteId == n2 && e.TargetNoteId == n1));
        body.Edges.Should().NotContain(e =>
            (e.SourceNoteId == n1 && e.TargetNoteId == n3) ||
            (e.SourceNoteId == n3 && e.TargetNoteId == n1));
    }

    [Fact]
    public async Task Get_Graph_Filtered_By_Workspace()
    {
        var workspaceA = await CreateWorkspaceAsync();
        var workspaceB = await CreateWorkspaceAsync();
        await CreateNoteAsync(workspaceA, "A1", ["shared"]);
        await CreateNoteAsync(workspaceA, "A2", ["shared"]);
        await CreateNoteAsync(workspaceB, "B1", ["shared"]);

        var onlyA = await (await _client.GetAsync($"/api/graph?workspaceId={workspaceA}")).Content.ReadFromJsonAsync<GraphBody>();
        onlyA!.Nodes.Should().HaveCount(2);
        onlyA.Edges.Should().HaveCount(1);
    }

    public sealed record GraphNodeBody(Guid Id, string Title, Guid WorkspaceId, string WorkspaceName, IReadOnlyList<string> Tags);
    public sealed record GraphEdgeBody(Guid SourceNoteId, Guid TargetNoteId);
    public sealed record GraphBody(IReadOnlyList<GraphNodeBody> Nodes, IReadOnlyList<GraphEdgeBody> Edges);
}