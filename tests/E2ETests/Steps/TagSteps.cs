using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using TechTalk.SpecFlow;

namespace KnowledgeBase.E2ETests.Steps;

[Binding]
public sealed class TagSteps
{
    private readonly HttpClient _client;
    private Guid _tagId;
    private readonly List<Guid> _noteIds = [];

    public TagSteps()
    {
        _client = ApiSteps.CurrentClient;
    }

    [Given(@"a tag named ""(.*)"" with color ""(.*)""")]
    public async Task GivenATagNamedWithColor(string name, string color)
    {
        var response = await _client.PostAsJsonAsync("/api/tags", new { name, color });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        _tagId = await response.Content.ReadFromJsonAsync<Guid>();
    }

    [When(@"I rename that tag to ""(.*)"" with color ""(.*)""")]
    public async Task WhenIRenameThatTagToWithColor(string name, string color)
    {
        var response = await _client.PutAsJsonAsync($"/api/tags/{_tagId}", new { name, color });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Then(@"the tag ""(.*)"" exists with color ""(.*)""")]
    public async Task ThenTheTagExistsWithColor(string name, string color)
    {
        var tags = await (await _client.GetAsync("/api/tags")).Content.ReadFromJsonAsync<List<TagBody>>();
        tags!.Should().ContainSingle(t => t.Name == name && t.Color == color);
    }

    [Then(@"the tag ""(.*)"" does not exist")]
    public async Task ThenTheTagDoesNotExist(string name)
    {
        var tags = await (await _client.GetAsync("/api/tags")).Content.ReadFromJsonAsync<List<TagBody>>();
        tags!.Should().NotContain(t => t.Name == name);
    }

    [Given(@"a note tagged ""(.*)"" in that workspace")]
    public async Task GivenANoteTaggedInThatWorkspace(string tagsCsv)
    {
        var tags = tagsCsv.Split(',').Select(t => t.Trim()).ToArray();
        var response = await _client.PostAsJsonAsync($"/api/workspaces/{ApiSteps.CurrentWorkspaceId}/notes", new
        {
            title = $"Note-{_noteIds.Count + 1}",
            contentMarkdown = "# body",
            tags
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        _noteIds.Add(await response.Content.ReadFromJsonAsync<Guid>());
    }

    [When(@"I merge tag ""(.*)"" into tag ""(.*)""")]
    public async Task WhenIMergeTagIntoTag(string sourceName, string targetName)
    {
        var tags = await (await _client.GetAsync("/api/tags")).Content.ReadFromJsonAsync<List<TagBody>>();
        var source = tags!.Single(t => t.Name == sourceName);
        var target = tags.Single(t => t.Name == targetName);

        var response = await _client.PostAsJsonAsync("/api/tag-merges", new
        {
            sourceTagId = source.Id,
            targetTagId = target.Id
        });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Then(@"the tag ""(.*)"" has (\d+) notes")]
    public async Task ThenTheTagHasNotes(string name, int count)
    {
        var tags = await (await _client.GetAsync("/api/tags")).Content.ReadFromJsonAsync<List<TagBody>>();
        tags!.Single(t => t.Name == name).NoteCount.Should().Be(count);
    }

    public sealed record TagBody(Guid Id, string Name, string Color, int NoteCount);
}