using AwesomeAssertions;
using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.Features.Notes;
using KnowledgeBase.Application.Features.Tags;
using KnowledgeBase.Application.Features.Workspaces;
using KnowledgeBase.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KnowledgeBase.UnitTests.Features.Notes;

public sealed class NoteServiceTests
{
    private static async Task<Guid> CreateWorkspaceAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var workspaces = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        return (await workspaces.CreateAsync(new CreateWorkspaceCommand("Dev", null), CancellationToken.None)).Value;
    }

    [Fact]
    public async Task Create_Note_With_New_Tags_Creates_Tags_And_Links()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<INoteService>();
        var workspaceId = await CreateWorkspaceAsync(fixture.Provider);

        var result = await service.CreateAsync(
            new CreateNoteCommand(workspaceId, "Notes title", "# body", ["dotnet", "database"]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var fetched = await service.GetByIdAsync(result.Value, CancellationToken.None);
        fetched.Value!.Tags.Select(t => t.Name).Should().BeEquivalentTo(["dotnet", "database"]);
    }

    [Fact]
    public async Task Create_Note_Reuses_Existing_Tags()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<INoteService>();
        var tags = scope.ServiceProvider.GetRequiredService<ITagService>();
        var workspaceId = await CreateWorkspaceAsync(fixture.Provider);

        await tags.CreateAsync(new CreateTagCommand("dotnet", "#512BD4"), CancellationToken.None);

        await service.CreateAsync(
            new CreateNoteCommand(workspaceId, "One", "body", ["dotnet"]),
            CancellationToken.None);
        var result = await service.CreateAsync(
            new CreateNoteCommand(workspaceId, "Two", "body", ["dotnet"]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        using (var countScope = fixture.Provider.CreateAsyncScope())
        {
            var db = countScope.ServiceProvider.GetRequiredService<IAppDbContext>();
            (await db.Tags.CountAsync(t => t.Name == "dotnet")).Should().Be(1);
        }
    }

    [Fact]
    public async Task Create_Note_In_Unknown_Workspace_Returns_NotFound()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<INoteService>();

        var result = await service.CreateAsync(
            new CreateNoteCommand(Guid.NewGuid(), "x", "body", []),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task Update_Note_Replaces_Tags()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<INoteService>();
        var workspaceId = await CreateWorkspaceAsync(fixture.Provider);

        var noteId = (await service.CreateAsync(
            new CreateNoteCommand(workspaceId, "Title", "old", ["csharp"]),
            CancellationToken.None)).Value;

        var result = await service.UpdateAsync(
            new UpdateNoteCommand(noteId, "New title", "# new", ["dotnet", "efcore"]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var fetched = await service.GetByIdAsync(noteId, CancellationToken.None);
        fetched.Value!.Title.Should().Be("New title");
        fetched.Value.ContentMarkdown.Should().Be("# new");
        fetched.Value.Tags.Select(t => t.Name).Should().BeEquivalentTo(["dotnet", "efcore"]);
    }

    [Fact]
    public async Task Update_Unknown_Note_Returns_NotFound()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<INoteService>();

        var result = await service.UpdateAsync(
            new UpdateNoteCommand(Guid.NewGuid(), "x", "body", []),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task Delete_Note_Removes_Its_Tag_Links()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<INoteService>();
        var workspaceId = await CreateWorkspaceAsync(fixture.Provider);

        var noteId = (await service.CreateAsync(
            new CreateNoteCommand(workspaceId, "Title", "body", ["x", "y"]),
            CancellationToken.None)).Value;

        var result = await service.DeleteAsync(new DeleteNoteCommand(noteId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        using (var countScope = fixture.Provider.CreateAsyncScope())
        {
            var db = countScope.ServiceProvider.GetRequiredService<IAppDbContext>();
            (await db.Notes.CountAsync(n => n.Id == noteId)).Should().Be(0);
            (await db.NoteTags.CountAsync(nt => nt.NoteId == noteId)).Should().Be(0);
            (await db.Tags.CountAsync()).Should().Be(2);
        }
    }

    [Fact]
    public async Task Create_Note_Empty_Title_Returns_Invalid()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<INoteService>();
        var workspaceId = await CreateWorkspaceAsync(fixture.Provider);

        var result = await service.CreateAsync(
            new CreateNoteCommand(workspaceId, "", "body", []),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Invalid");
    }
}