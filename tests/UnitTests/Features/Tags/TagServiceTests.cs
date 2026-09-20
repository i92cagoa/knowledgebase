using AwesomeAssertions;
using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.Features.Notes;
using KnowledgeBase.Application.Features.Tags;
using KnowledgeBase.Application.Features.Workspaces;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KnowledgeBase.UnitTests.Features.Tags;

public sealed class TagServiceTests
{
    [Fact]
    public async Task Create_Auto_Generates_Id()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();

        var result = await service.CreateAsync(new CreateTagCommand("dotnet", "#512BD4"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var tag = await db.Tags.FindAsync([result.Value]);
        tag!.Name.Should().Be("dotnet");
        tag.Color.Should().Be("#512BD4");
    }

    [Fact]
    public async Task Create_Duplicate_Name_Returns_Conflict()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();

        await service.CreateAsync(new CreateTagCommand("dotnet", "#000000"), CancellationToken.None);
        var result = await service.CreateAsync(new CreateTagCommand("dotnet", "#512BD4"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task Create_Invalid_Color_Returns_Invalid()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();

        var result = await service.CreateAsync(new CreateTagCommand("dotnet", "red"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Invalid");
    }

    [Fact]
    public async Task List_Returns_Alphabetical_Tags()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        db.Tags.Add(Tag.Create("zebra"));
        db.Tags.Add(Tag.Create("apple", "#ff0000"));
        await db.SaveChangesAsync();

        var result = await service.ListAsync(CancellationToken.None);

        result.Value!.Select(t => t.Name).Should().Equal("apple", "zebra");
    }

    [Fact]
    public async Task Delete_Unknown_Returns_NotFound()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();

        var result = await service.DeleteAsync(new DeleteTagCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task Update_Renames_And_Recolors()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();

        var id = (await service.CreateAsync(new CreateTagCommand("before", "#000000"), CancellationToken.None)).Value;
        var result = await service.UpdateAsync(new UpdateTagCommand(id, "after", "#123456"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var list = await service.ListAsync(CancellationToken.None);
        list.Value!.Should().ContainSingle(t => t.Id == id && t.Name == "after" && t.Color == "#123456");
    }

    [Fact]
    public async Task Update_To_Existing_Name_Returns_Conflict()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();

        await service.CreateAsync(new CreateTagCommand("keep", "#000000"), CancellationToken.None);
        var id = (await service.CreateAsync(new CreateTagCommand("other", "#000000"), CancellationToken.None)).Value;

        var result = await service.UpdateAsync(new UpdateTagCommand(id, "keep", "#000000"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task Update_Unknown_Returns_NotFound()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();

        var result = await service.UpdateAsync(new UpdateTagCommand(Guid.NewGuid(), "x", "#000000"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task Update_Invalid_Color_Returns_Invalid()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();

        var id = (await service.CreateAsync(new CreateTagCommand("t", "#000000"), CancellationToken.None)).Value;
        var result = await service.UpdateAsync(new UpdateTagCommand(id, "n", "red"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Invalid");
    }

    [Fact]
    public async Task Merge_Moves_Notes_To_Target_And_Deletes_Source()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();
        var notesService = scope.ServiceProvider.GetRequiredService<INoteService>();
        var workspacesService = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();

        var workspaceId = (await workspacesService.CreateAsync(new CreateWorkspaceCommand("Merge", null), CancellationToken.None)).Value;
        await notesService.CreateAsync(new CreateNoteCommand(workspaceId, "A", "x", ["backend", "old"]), CancellationToken.None);
        await notesService.CreateAsync(new CreateNoteCommand(workspaceId, "B", "y", ["backend", "old"]), CancellationToken.None);

        var tags = await service.ListAsync(CancellationToken.None);
        var source = tags.Value!.Single(t => t.Name == "old");
        var target = tags.Value.Single(t => t.Name == "backend");

        var result = await service.MergeAsync(new MergeTagCommand(source.Id, target.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var after = await service.ListAsync(CancellationToken.None);
        after.Value!.Should().NotContain(t => t.Id == source.Id);
        after.Value.Single(t => t.Id == target.Id).NoteCount.Should().Be(2);
    }

    [Fact]
    public async Task Merge_Into_Itself_Returns_Invalid()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();

        var id = (await service.CreateAsync(new CreateTagCommand("solo", "#000000"), CancellationToken.None)).Value;
        var result = await service.MergeAsync(new MergeTagCommand(id, id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Invalid");
    }

    [Fact]
    public async Task List_Reports_Note_Counts()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();
        var notesService = scope.ServiceProvider.GetRequiredService<INoteService>();
        var workspacesService = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();

        var workspaceId = (await workspacesService.CreateAsync(new CreateWorkspaceCommand("Count", null), CancellationToken.None)).Value;
        await notesService.CreateAsync(new CreateNoteCommand(workspaceId, "N1", "x", ["dotnet"]), CancellationToken.None);
        await notesService.CreateAsync(new CreateNoteCommand(workspaceId, "N2", "y", ["dotnet"]), CancellationToken.None);

        var tags = await service.ListAsync(CancellationToken.None);

        tags.Value!.Single(t => t.Name == "dotnet").NoteCount.Should().Be(2);
    }
}