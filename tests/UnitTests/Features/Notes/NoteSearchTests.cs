using AwesomeAssertions;
using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.Features.Notes;
using KnowledgeBase.Application.Features.Workspaces;
using KnowledgeBase.UnitTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KnowledgeBase.UnitTests.Features.Notes;

public sealed class NoteSearchTests
{
    private static async Task<(Guid workspaceId, Guid notes, Guid dotnetNote, Guid databaseNote)> SeedAsync(
        TestAppDbContext fixture)
    {
        await using var scope = fixture.Provider.CreateAsyncScope();
        var workspaces = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();

        var workspaceId = (await workspaces.CreateAsync(new CreateWorkspaceCommand("Search", null), CancellationToken.None)).Value;

        var str = "search-notes";
        var firstId = (await notes.CreateAsync(
            new CreateNoteCommand(workspaceId, "EF Core caching", "covers Npgsql query plan", ["server", "dotnet"]),
            CancellationToken.None)).Value;
        var dotnetId = (await notes.CreateAsync(
            new CreateNoteCommand(workspaceId, "Avalonia list box", "binding example", ["ui", "dotnet"]),
            CancellationToken.None)).Value;
        var databaseId = (await notes.CreateAsync(
            new CreateNoteCommand(workspaceId, "Postgres indexes", "gin for full text", ["database", "caching"]),
            CancellationToken.None)).Value;

        return (workspaceId, firstId, dotnetId, databaseId);
    }

    [Fact]
    public async Task Search_By_Title_Returns_Matching_Notes()
    {
        using var fixture = new TestAppDbContext();
        var (_, _, dotnetId, _) = await SeedAsync(fixture);

        await using var scope = fixture.Provider.CreateAsyncScope();
        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();

        var result = await notes.SearchAsync(
            new SearchNotesCommand(TitleQuery: "Avalonia", PageSize: 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Single().Id.Should().Be(dotnetId);
        result.Value.Items.Single().Tags.Should().Contain("dotnet");
    }

    [Fact]
    public async Task Search_By_Content_Matches_Body()
    {
        using var fixture = new TestAppDbContext();
        var (_, _, _, databaseId) = await SeedAsync(fixture);

        await using var scope = fixture.Provider.CreateAsyncScope();
        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();

        var result = await notes.SearchAsync(
            new SearchNotesCommand(TitleQuery: "gin for full text", PageSize: 20),
            CancellationToken.None);

        result.Value!.Items.Should().ContainSingle(n => n.Id == databaseId);
    }

    [Fact]
    public async Task Search_By_Tag_Filters_By_Tag()
    {
        using var fixture = new TestAppDbContext();
        var (_, _, _, _) = await SeedAsync(fixture);

        await using var scope = fixture.Provider.CreateAsyncScope();
        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();

        var result = await notes.SearchAsync(
            new SearchNotesCommand(TagNames: ["database"], PageSize: 20),
            CancellationToken.None);

        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Single().Title.Should().Be("Postgres indexes");
    }

    [Fact]
    public async Task Search_With_No_Query_Returns_All_Paged()
    {
        using var fixture = new TestAppDbContext();
        var _ = await SeedAsync(fixture);

        await using var scope = fixture.Provider.CreateAsyncScope();
        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();

        var result = await notes.SearchAsync(
            new SearchNotesCommand(Page: 1, PageSize: 2),
            CancellationToken.None);

        result.Value!.TotalCount.Should().Be(3);
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalPages.Should().Be(2);

        var page2 = await notes.SearchAsync(
            new SearchNotesCommand(Page: 2, PageSize: 2),
            CancellationToken.None);

        page2.Value!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Search_Unknown_Tag_Returns_Empty()
    {
        using var fixture = new TestAppDbContext();
        var _ = await SeedAsync(fixture);

        await using var scope = fixture.Provider.CreateAsyncScope();
        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();

        var result = await notes.SearchAsync(
            new SearchNotesCommand(TagNames: ["nonexistent"]),
            CancellationToken.None);

        result.Value!.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Search_Orders_By_UpdatedAt_Descending()
    {
        using var fixture = new TestAppDbContext();
        var _ = await SeedAsync(fixture);

        await using var scope = fixture.Provider.CreateAsyncScope();
        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();

        var result = await notes.SearchAsync(
            new SearchNotesCommand(PageSize: 50),
            CancellationToken.None);

        var dates = result.Value!.Items.Select(i => i.UpdatedAt).ToList();
        dates.Should().BeInDescendingOrder();
    }
}