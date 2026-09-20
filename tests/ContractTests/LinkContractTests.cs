using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using KnowledgeBase.Application.Features.Links;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace KnowledgeBase.ContractTests.Features;

public sealed class LinkContractTests : IClassFixture<ContractTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LinkContractTests(ContractTestWebApplicationFactory factory)
    {
        _client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILinkContentFetcher>();
                    services.AddSingleton<ILinkContentFetcher>(new FakeLinkFetcher());
                });
            })
            .CreateClient();
    }

    private async Task<Guid> CreateWorkspaceAsync()
    {
        var name = $"Link-{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/api/workspaces", new { name, description = (string?)null });
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task Import_Link_Creates_Note()
    {
        var workspaceId = await CreateWorkspaceAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/links",
            new { url = "https://example.com/article" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var noteId = await response.Content.ReadFromJsonAsync<Guid>();

        var note = await (await _client.GetAsync($"/api/notes/{noteId}")).Content.ReadFromJsonAsync<NoteBody>();
        note!.SourceUrl.Should().Be("https://example.com/article");
        note.Tags.Select(t => t.Name).Should().Contain("postgres");
    }

    [Fact]
    public async Task Import_Link_Into_Unknown_Workspace_Returns_NotFound()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/workspaces/00000000-0000-0000-0000-000000000000/links",
            new { url = "https://example.com/a" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Import_Invalid_Url_Returns_BadRequest()
    {
        var workspaceId = await CreateWorkspaceAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/links",
            new { url = "not a url" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed class FakeLinkFetcher : ILinkContentFetcher
    {
        public Task<FetchedLink> FetchAsync(Uri url, CancellationToken cancellationToken) =>
            Task.FromResult(new FetchedLink(
                "Postgres at scale",
                "Postgres caching improves production databases. Postgres developers rely on caching.",
                "Postgres caching improves production databases."));
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