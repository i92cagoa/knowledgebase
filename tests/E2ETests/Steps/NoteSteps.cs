using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using TechTalk.SpecFlow;

namespace KnowledgeBase.E2ETests.Steps;

[Binding]
public sealed class NoteSteps
{
    private readonly NoteContext _context;
    private readonly HttpClient _client;

    private Guid _noteId;
    private string _noteTitle = "";

    public NoteSteps(NoteContext context)
    {
        _context = context;
        _client = ApiSteps.CurrentClient;
    }

    [When(@"I create a note titled ""(.*)"" in that workspace")]
    public async Task WhenICreateANoteTitledInThatWorkspace(string title)
    {
        _noteTitle = title;
        var response = await _client.PostAsJsonAsync($"/api/workspaces/{ApiSteps.CurrentWorkspaceId}/notes", new
        {
            title,
            contentMarkdown = "# body",
            tags = Array.Empty<string>()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        _noteId = await response.Content.ReadFromJsonAsync<Guid>();
    }

    [Then(@"the note is stored under the workspace")]
    public async Task ThenTheNoteIsStoredUnderTheWorkspace()
    {
        var tree = await (await _client.GetAsync("/api/workspaces?embed=notes")).Content.ReadFromJsonAsync<List<TreeBody>>();
        var ws = tree!.Single(w => w.Id == ApiSteps.CurrentWorkspaceId);
        ws.Notes.Should().Contain(n => n.Id == _noteId);
    }

    [When(@"I add tags ""(.*)"" to the note")]
    public async Task WhenIAddTagsToTheNote(string tagsCsv)
    {
        var tags = tagsCsv.Split(',').Select(t => t.Trim()).ToArray();
        var response = await _client.PutAsJsonAsync($"/api/notes/{_noteId}", new
        {
            title = _noteTitle,
            contentMarkdown = "# body",
            tags
        });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Then(@"the note has tags ""(.*)""")]
    public async Task ThenTheNoteHasTags(string tagsCsv)
    {
        var note = await (await _client.GetAsync($"/api/notes/{_noteId}")).Content.ReadFromJsonAsync<NoteBody>();
        var expected = tagsCsv.Split(',').Select(t => t.Trim()).ToArray();
        note!.Tags.Select(t => t.Name).Should().BeEquivalentTo(expected);
    }

    [Then(@"the tag ""(.*)"" exists exactly once")]
    public async Task ThenTheTagExistsExactlyOnce(string tagName)
    {
        var tags = await (await _client.GetAsync("/api/tags")).Content.ReadFromJsonAsync<List<TagBody>>();
        tags!.Count(t => t.Name == tagName).Should().Be(1);
    }

    [Then(@"searching for ""(.*)"" returns (\d+) note")]
    public async Task ThenSearchingForReturnsNote(string query, int expectedCount)
    {
        var page = await (await _client.GetAsync($"/api/notes?titleQuery={Uri.EscapeDataString(query)}")).Content.ReadFromJsonAsync<SearchPage>();
        page!.TotalCount.Should().Be(expectedCount);
    }

    [Then(@"searching for tag ""(.*)"" returns (\d+) note")]
    public async Task ThenSearchingForTagReturnsNote(string tag, int expectedCount)
    {
        var page = await (await _client.GetAsync($"/api/notes?tags={Uri.EscapeDataString(tag)}")).Content.ReadFromJsonAsync<SearchPage>();
        page!.TotalCount.Should().Be(expectedCount);
    }

    public sealed record SearchItem(Guid Id, string Title, DateTime UpdatedAt, Guid WorkspaceId, string WorkspaceName, IReadOnlyList<string> Tags);
    public sealed record SearchPage(IReadOnlyList<SearchItem> Items, int Page, int PageSize, int TotalCount, int TotalPages);

    public sealed record TreeBody(Guid Id, string Name, IReadOnlyList<NoteTreeBody> Notes);
    public sealed record NoteTreeBody(Guid Id, string Title, DateTime UpdatedAt, IReadOnlyList<string> Tags);
    public sealed record NoteBody(
        Guid Id,
        string Title,
        string ContentMarkdown,
        Guid WorkspaceId,
        IReadOnlyList<TagBody> Tags,
        IReadOnlyList<object> Attachments);
    public sealed record TagBody(Guid Id, string Name, string Color, int NoteCount);
}