using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TechTalk.SpecFlow;

namespace KnowledgeBase.E2ETests.Steps;

[Binding]
public sealed class NoteSteps
{
    private readonly NoteContext _context;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private Guid _workspaceId;
    private Guid _noteId;

    public NoteSteps(NoteContext context)
    {
        _context = context;
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"kb-e2e-{Guid.NewGuid():N}.db");
            _storagePath = Path.Combine(Path.GetTempPath(), $"kb-e2e-uploads-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_storagePath);
            builder.UseSetting("Database:Provider", "Sqlite");
            builder.UseSetting("Database:ConnectionString", $"Data Source={_dbPath}");
            builder.UseSetting("Storage:RootPath", _storagePath);
        });
        _client = _factory.CreateClient();
    }

    private string _dbPath = "";
    private string _storagePath = "";

    [Given(@"the API is running with a clean database")]
    public void GivenTheApiIsRunningWithCleanDatabase()
    {
        _client.BaseAddress.Should().NotBeNull();
    }

    [Given(@"a workspace named ""(.*)""")]
    public async Task GivenAWorkspaceNamed(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/workspaces", new { name, description = (string?)null });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        _workspaceId = await response.Content.ReadFromJsonAsync<Guid>();
    }

    [When(@"I create a note titled ""(.*)"" in that workspace")]
    public async Task WhenICreateANoteTitledInThatWorkspace(string title)
    {
        var response = await _client.PostAsJsonAsync($"/api/workspaces/{_workspaceId}/notes", new
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
        var ws = tree!.Single(w => w.Id == _workspaceId);
        ws.Notes.Should().Contain(n => n.Id == _noteId);
    }

    [When(@"I add tags ""(.*)"" to the note")]
    public async Task WhenIAddTagsToTheNote(string tagsCsv)
    {
        var tags = tagsCsv.Split(',').Select(t => t.Trim()).ToArray();
        var response = await _client.PutAsJsonAsync($"/api/notes/{_noteId}", new
        {
            title = "title",
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

    public sealed record TreeBody(Guid Id, string Name, IReadOnlyList<NoteTreeBody> Notes);
    public sealed record NoteTreeBody(Guid Id, string Title, DateTime UpdatedAt, IReadOnlyList<string> Tags);
    public sealed record NoteBody(
        Guid Id,
        string Title,
        string ContentMarkdown,
        Guid WorkspaceId,
        IReadOnlyList<TagBody> Tags,
        IReadOnlyList<object> Attachments);
    public sealed record TagBody(Guid Id, string Name, string Color);
}