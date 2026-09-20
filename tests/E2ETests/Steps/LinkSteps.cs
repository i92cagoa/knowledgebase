using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using TechTalk.SpecFlow;

namespace KnowledgeBase.E2ETests.Steps;

[Binding]
public sealed class LinkSteps
{
    private readonly HttpClient _client;
    private Guid _noteId;

    public LinkSteps()
    {
        _client = ApiSteps.CurrentClient;
    }

    [When(@"I import the link ""(.*)""")]
    public async Task WhenIImportTheLink(string url)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/workspaces/{ApiSteps.CurrentWorkspaceId}/links",
            new { url });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        _noteId = await response.Content.ReadFromJsonAsync<Guid>();
    }

    [Then(@"a note exists with source ""(.*)""")]
    public async Task ThenANoteExistsWithSource(string url)
    {
        var note = await (await _client.GetAsync($"/api/notes/{_noteId}")).Content.ReadFromJsonAsync<NoteBody>();
        note!.SourceUrl.Should().Be(url);
    }

    [Then(@"that note has the summary of the article")]
    public async Task ThenThatNoteHasTheSummaryOfTheArticle()
    {
        var note = await (await _client.GetAsync($"/api/notes/{_noteId}")).Content.ReadFromJsonAsync<NoteBody>();
        note!.SourceSummary.Should().Contain("Postgres caching");
    }

    [Then(@"that note has the tag ""(.*)""")]
    public async Task ThenThatNoteHasTheTag(string tag)
    {
        var note = await (await _client.GetAsync($"/api/notes/{_noteId}")).Content.ReadFromJsonAsync<NoteBody>();
        note!.Tags.Select(t => t.Name).Should().Contain(tag);
    }

    public sealed record TagBody(Guid Id, string Name, string Color);
    public sealed record NoteBody(
        Guid Id,
        string Title,
        string ContentMarkdown,
        Guid WorkspaceId,
        string? SourceUrl,
        string? SourceSummary,
        IReadOnlyList<TagBody> Tags,
        IReadOnlyList<object> Attachments);
}