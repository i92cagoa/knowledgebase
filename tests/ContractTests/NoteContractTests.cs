using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Xunit;

namespace KnowledgeBase.ContractTests.Features;

public sealed class NoteContractTests : IClassFixture<ContractTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public NoteContractTests(ContractTestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateWorkspaceAsync()
    {
        var name = $"Dev-{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/api/workspaces", new { name, description = (string?)null });
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> CreateNoteAsync(Guid workspaceId, string title, string contentMarkdown, string[] tags)
    {
        var response = await _client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/notes", new
        {
            title,
            contentMarkdown,
            tags
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task Create_Note_With_Tags_Then_Get_Tree()
    {
        var workspaceId = await CreateWorkspaceAsync();
        var id = await CreateNoteAsync(workspaceId, "EF Core tips", "# Title\n\nSome **bold** text", ["dotnet", "database"]);

        var note = await (await _client.GetAsync($"/api/notes/{id}")).Content.ReadFromJsonAsync<NoteBody>();
        note!.Title.Should().Be("EF Core tips");
        note.Tags.Select(t => t.Name).Should().BeEquivalentTo("dotnet", "database");

        var tree = await (await _client.GetAsync("/api/workspaces?embed=notes")).Content.ReadFromJsonAsync<List<TreeBody>>();
        tree!.Should().ContainSingle(w => w.Id == workspaceId);
        tree.First(w => w.Id == workspaceId).Notes.Should().ContainSingle(n => n.Id == id);
    }

    [Fact]
    public async Task Create_Note_In_Workspace_Then_List_By_Workspace()
    {
        var workspaceId = await CreateWorkspaceAsync();
        var id = await CreateNoteAsync(workspaceId, "Nested", "body", []);

        var list = await (await _client.GetAsync($"/api/workspaces/{workspaceId}/notes")).Content.ReadFromJsonAsync<List<NoteTreeBody>>();
        list!.Should().ContainSingle(n => n.Id == id);
    }

    [Fact]
    public async Task Update_Note_Replaces_Tags()
    {
        var workspaceId = await CreateWorkspaceAsync();
        var id = await CreateNoteAsync(workspaceId, "Old", "old", ["csharp"]);

        var update = await _client.PutAsJsonAsync($"/api/notes/{id}", new
        {
            title = "New",
            contentMarkdown = "# New",
            tags = new[] { "dotnet" }
        });

        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var note = await (await _client.GetAsync($"/api/notes/{id}")).Content.ReadFromJsonAsync<NoteBody>();
        note!.Title.Should().Be("New");
        note.Tags.Should().ContainSingle(t => t.Name == "dotnet");
    }

    [Fact]
    public async Task Delete_Note()
    {
        var workspaceId = await CreateWorkspaceAsync();
        var id = await CreateNoteAsync(workspaceId, "Temp", "temp", []);

        var delete = await _client.DeleteAsync($"/api/notes/{id}");

        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/notes/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Note_Validation_Failure_Returns_BadRequest()
    {
        var workspaceId = await CreateWorkspaceAsync();
        var response = await _client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/notes", new
        {
            title = "",
            contentMarkdown = "body",
            tags = Array.Empty<string>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public sealed record TagBody(Guid Id, string Name, string Color);
    public sealed record NoteBody(
        Guid Id,
        string Title,
        string ContentMarkdown,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        Guid WorkspaceId,
        string? SourceUrl,
        string? SourceSummary,
        IReadOnlyList<TagBody> Tags,
        IReadOnlyList<AttachmentBody> Attachments);
    public sealed record AttachmentBody(Guid Id, string FileName, string ContentType, int Kind, string Url);

    public sealed record NoteTreeBody(Guid Id, string Title, DateTime UpdatedAt, IReadOnlyList<string> Tags);
    public sealed record TreeBody(Guid Id, string Name, IReadOnlyList<NoteTreeBody> Notes);
}