using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Xunit;

namespace KnowledgeBase.ContractTests.Features;

public sealed class NoteSearchContractTests : IClassFixture<ContractTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public NoteSearchContractTests(ContractTestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateWorkspaceAsync()
    {
        var name = $"Search-{Guid.NewGuid():N}";
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
    public async Task Search_Notes_By_Title_And_Tag_With_Pagination()
    {
        var workspaceId = await CreateWorkspaceAsync();
        await CreateNoteAsync(workspaceId, "EF Core caching", "covers provider model", ["dotnet", "database"]);
        await CreateNoteAsync(workspaceId, "Avalonia bindings", "xaml example", ["dotnet", "ui"]);
        await CreateNoteAsync(workspaceId, "Postgres indexes", "gin", ["database"]);

        var byTitle = await _client.GetAsync("/api/notes?titleQuery=Avalonia");
        byTitle.StatusCode.Should().Be(HttpStatusCode.OK);
        var titlePage = await byTitle.Content.ReadFromJsonAsync<SearchPage>();
        titlePage!.TotalCount.Should().Be(1);
        titlePage.Items.Should().ContainSingle(n => n.Title == "Avalonia bindings");

        var byTag = await _client.GetAsync("/api/notes?tags=database");
        var tagPage = await byTag.Content.ReadFromJsonAsync<SearchPage>();
        tagPage!.TotalCount.Should().Be(2);
        tagPage.Items.Select(n => n.Title).Should().Contain("EF Core caching").And.Contain("Postgres indexes");

        var paged = await _client.GetAsync("/api/notes?page=1&pageSize=2");
        var pagedBody = await paged.Content.ReadFromJsonAsync<SearchPage>();
        pagedBody!.Items.Should().HaveCount(2);
        pagedBody.TotalCount.Should().Be(3);
        pagedBody.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Search_Unknown_Tag_Returns_Empty_Page()
    {
        var response = await _client.GetAsync("/api/notes?tags=nope");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SearchPage>();
        body!.TotalCount.Should().Be(0);
        body.Items.Should().BeEmpty();
    }

    public sealed record SearchItem(Guid Id, string Title, DateTime UpdatedAt, Guid WorkspaceId, string WorkspaceName, IReadOnlyList<string> Tags);
    public sealed record SearchPage(IReadOnlyList<SearchItem> Items, int Page, int PageSize, int TotalCount, int TotalPages);
}