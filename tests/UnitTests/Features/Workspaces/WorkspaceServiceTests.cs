using AwesomeAssertions;
using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.Features.Workspaces;
using KnowledgeBase.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KnowledgeBase.UnitTests.Features.Workspaces;

public sealed class WorkspaceServiceTests
{
    [Fact]
    public async Task Create_And_Get_Workspace()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();

        var result = await service.CreateAsync(new CreateWorkspaceCommand("Testing", "unit"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var fetched = await service.GetByIdAsync(result.Value, CancellationToken.None);
        fetched.IsSuccess.Should().BeTrue();
        fetched.Value!.Name.Should().Be("Testing");
        fetched.Value.Description.Should().Be("unit");
    }

    [Fact]
    public async Task Create_Duplicate_Name_Returns_Conflict()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();

        var first = await service.CreateAsync(new CreateWorkspaceCommand("Dupe", null), CancellationToken.None);
        var second = await service.CreateAsync(new CreateWorkspaceCommand("Dupe", null), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeFalse();
        second.Error.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task Create_Empty_Name_Returns_Invalid()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();

        var result = await service.CreateAsync(new CreateWorkspaceCommand("", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Invalid");
    }

    [Fact]
    public async Task Update_Workspace()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();

        var id = (await service.CreateAsync(new CreateWorkspaceCommand("Before", null), CancellationToken.None)).Value;

        var result = await service.UpdateAsync(new UpdateWorkspaceCommand(id, "After", "desc"), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        var fetched = await service.GetByIdAsync(id, CancellationToken.None);
        fetched.Value!.Name.Should().Be("After");
        fetched.Value.Description.Should().Be("desc");
    }

    [Fact]
    public async Task Update_Unknown_Returns_NotFound()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();

        var result = await service.UpdateAsync(new UpdateWorkspaceCommand(Guid.NewGuid(), "X", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task Delete_Workspace()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();

        var id = (await service.CreateAsync(new CreateWorkspaceCommand("Delete", null), CancellationToken.None)).Value;
        var result = await service.DeleteAsync(new DeleteWorkspaceCommand(id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        using (var countScope = fixture.Provider.CreateAsyncScope())
        {
            var db = countScope.ServiceProvider.GetRequiredService<IAppDbContext>();
            (await db.Workspaces.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task List_And_Tree_Return_Alphabetical()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();

        await service.CreateAsync(new CreateWorkspaceCommand("Zulu", null), CancellationToken.None);
        await service.CreateAsync(new CreateWorkspaceCommand("Alpha", null), CancellationToken.None);

        var list = await service.ListAsync(CancellationToken.None);
        list.Value!.Select(w => w.Name).Should().Equal("Alpha", "Zulu");

        var tree = await service.GetTreeAsync(CancellationToken.None);
        tree.Value!.Should().HaveCount(2);
    }
}