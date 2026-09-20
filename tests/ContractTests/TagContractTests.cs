using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Xunit;

namespace KnowledgeBase.ContractTests.Features;

public sealed class TagContractTests : IClassFixture<ContractTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TagContractTests(ContractTestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_Create_Delete_Tags()
    {
        var list = await (await _client.GetAsync("/api/tags")).Content.ReadFromJsonAsync<List<object>>();
        list.Should().BeEmpty();

        var create = await _client.PostAsJsonAsync("/api/tags", new { name = "dotnet", color = "#512BD4" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await create.Content.ReadFromJsonAsync<Guid>();

        await _client.GetAsync("/api/tags");

        var delete = await _client.DeleteAsync($"/api/tags/{id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Create_Tag_With_Invalid_Color_Returns_BadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/tags", new { name = "x", color = "red" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Tag_Renames_And_Recolors()
    {
        var id = await (await _client.PostAsJsonAsync("/api/tags", new { name = "num", color = "#000000" })).Content.ReadFromJsonAsync<Guid>();

        var response = await _client.PutAsJsonAsync($"/api/tags/{id}", new { name = "number", color = "#123456" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var tags = await (await _client.GetAsync("/api/tags")).Content.ReadFromJsonAsync<List<TagBody>>();
        tags!.Should().ContainSingle(t => t.Id == id && t.Name == "number" && t.Color == "#123456");
    }

    [Fact]
    public async Task Update_Tag_To_Existing_Name_Returns_Conflict()
    {
        await _client.PostAsJsonAsync("/api/tags", new { name = "keep", color = "#000000" });
        var id = await (await _client.PostAsJsonAsync("/api/tags", new { name = "other", color = "#000000" })).Content.ReadFromJsonAsync<Guid>();

        var response = await _client.PutAsJsonAsync($"/api/tags/{id}", new { name = "keep", color = "#000000" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<Guid> CreateWorkspaceAsync()
    {
        var name = $"Tag-{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/api/workspaces", new { name, description = (string?)null });
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> CreateNoteAsync(Guid workspaceId, string[] tags)
    {
        var response = await _client.PostAsJsonAsync($"/api/workspaces/{workspaceId}/notes", new
        {
            title = "Note",
            contentMarkdown = "body",
            tags
        });
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task Merge_Tag_Reassigns_Notes_And_Removes_Source()
    {
        var workspaceId = await CreateWorkspaceAsync();
        await CreateNoteAsync(workspaceId, ["backend", "old"]);
        await CreateNoteAsync(workspaceId, ["backend", "old"]);

        var tags = await (await _client.GetAsync("/api/tags")).Content.ReadFromJsonAsync<List<TagBody>>();
        var source = tags!.Single(t => t.Name == "old");
        var target = tags.Single(t => t.Name == "backend");

        var response = await _client.PostAsJsonAsync("/api/tag-merges", new { sourceTagId = source.Id, targetTagId = target.Id });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await (await _client.GetAsync("/api/tags")).Content.ReadFromJsonAsync<List<TagBody>>();
        after!.Should().NotContain(t => t.Id == source.Id);
        after.Single(t => t.Id == target.Id).NoteCount.Should().Be(2);
        after.Single(t => t.Id == target.Id).Name.Should().Be("backend");
    }

    [Fact]
    public async Task Merge_Tag_With_Itself_Returns_BadRequest()
    {
        var id = await (await _client.PostAsJsonAsync("/api/tags", new { name = "single", color = "#000000" })).Content.ReadFromJsonAsync<Guid>();

        var response = await _client.PostAsJsonAsync("/api/tag-merges", new { sourceTagId = id, targetTagId = id });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public sealed record TagBody(Guid Id, string Name, string Color, int NoteCount);
}