using AwesomeAssertions;
using KnowledgeBase.Application.Features.Links;
using KnowledgeBase.Application.Features.Notes;
using KnowledgeBase.Application.Features.Workspaces;
using KnowledgeBase.Infrastructure.Links;
using KnowledgeBase.UnitTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KnowledgeBase.UnitTests.Features.Links;

public sealed class LinkServiceTests
{
    [Fact]
    public async Task Import_Valid_Link_Creates_Note_With_Analysis()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var workspaces = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        var links = scope.ServiceProvider.GetRequiredService<ILinkService>();

        var workspaceId = (await workspaces.CreateAsync(new CreateWorkspaceCommand("Links", null), CancellationToken.None)).Value;

        var result = await links.ImportAsync(workspaceId, new ImportLinkCommand("https://example.com/article"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();
        var note = (await notes.GetByIdAsync(result.Value, CancellationToken.None)).Value!;
        note.SourceUrl.Should().Be("https://example.com/article");
        note.SourceSummary.Should().Contain("Postgres caching");
        note.Tags.Select(t => t.Name).Should().Contain("postgres");
    }

    [Fact]
    public async Task Import_Unknown_Workspace_Returns_NotFound()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var links = scope.ServiceProvider.GetRequiredService<ILinkService>();

        var result = await links.ImportAsync(Guid.NewGuid(), new ImportLinkCommand("https://example.com/a"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NotFound");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/x")]
    public async Task Import_Invalid_Url_Returns_Invalid(string url)
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var workspaces = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        var links = scope.ServiceProvider.GetRequiredService<ILinkService>();

        var workspaceId = (await workspaces.CreateAsync(new CreateWorkspaceCommand("Links", null), CancellationToken.None)).Value;

        var result = await links.ImportAsync(workspaceId, new ImportLinkCommand(url), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Invalid");
    }
}

public sealed class RulesLinkAnalyzerTests
{
    [Fact]
    public async Task Rules_Analyzer_Extracts_Keywords_And_Summary()
    {
        var analyzer = new RulesLinkAnalyzer();
        var link = new FetchedLink(
            "Postgres at scale",
            "Postgres caching is essential for production databases. Caching strategies improve latency. Postgres developers rely on caching every day.",
            "Postgres caching is essential for production databases.");

        var result = await analyzer.AnalyzeAsync(link, CancellationToken.None);

        result.Title.Should().Be("Postgres at scale");
        result.Summary.Should().Contain("Postgres caching");
        result.Tags.Should().Contain("postgres").And.Contain("caching");
    }

    [Fact]
    public async Task Rules_Analyzer_Excludes_StopWords()
    {
        var analyzer = new RulesLinkAnalyzer();
        var link = new FetchedLink("t", "the and or of a an is are to for", "t");

        var result = await analyzer.AnalyzeAsync(link, CancellationToken.None);

        result.Tags.Should().BeEmpty();
    }
}