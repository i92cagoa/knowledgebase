using AwesomeAssertions;
using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.Features.Notes;
using KnowledgeBase.Application.Features.Workspaces;
using KnowledgeBase.UnitTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KnowledgeBase.UnitTests.Features.Notes;

public sealed class NoteGraphTests
{
    private static async Task<Guid[]> CreateWorkspacesAsync(TestAppDbContext fixture, params string[] names)
    {
        await using var scope = fixture.Provider.CreateAsyncScope();
        var workspaces = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();

        var ids = new Guid[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            ids[i] = (await workspaces.CreateAsync(new CreateWorkspaceCommand(names[i], null), CancellationToken.None)).Value;
        }

        return ids;
    }

    private static async Task<Guid> CreateNoteAsync(
        TestAppDbContext fixture,
        Guid workspaceId,
        string title,
        params string[] tags)
    {
        await using var scope = fixture.Provider.CreateAsyncScope();
        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();

        return (await notes.CreateAsync(
            new CreateNoteCommand(workspaceId, title, "body", tags),
            CancellationToken.None)).Value;
    }

    [Fact]
    public async Task Graph_Connects_Notes_Sharing_Tags()
    {
        using var fixture = new TestAppDbContext();
        var workspace = (await CreateWorkspacesAsync(fixture, "Arch"))[0];

        var n1 = await CreateNoteAsync(fixture, workspace, "One", "dotnet");
        var n2 = await CreateNoteAsync(fixture, workspace, "Two", "dotnet", "database");
        var n3 = await CreateNoteAsync(fixture, workspace, "Three", "ui");

        await using var scope = fixture.Provider.CreateAsyncScope();
        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();

        var result = await notes.GetGraphAsync(null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var graph = result.Value!;
        graph.Nodes.Should().HaveCount(3);
        graph.Edges.Should().ContainSingle(e =>
            (e.SourceNoteId == n1 && e.TargetNoteId == n2) ||
            (e.SourceNoteId == n2 && e.TargetNoteId == n1));
        graph.Edges.Should().NotContain(e =>
            (e.SourceNoteId == n1 && e.TargetNoteId == n3) ||
            (e.SourceNoteId == n3 && e.TargetNoteId == n1));
    }

    [Fact]
    public async Task Graph_Edges_Have_No_Duplicates()
    {
        using var fixture = new TestAppDbContext();
        var workspace = (await CreateWorkspacesAsync(fixture, "Arch"))[0];

        await CreateNoteAsync(fixture, workspace, "A", "shared");
        await CreateNoteAsync(fixture, workspace, "B", "shared");
        await CreateNoteAsync(fixture, workspace, "C", "shared");

        await using var scope = fixture.Provider.CreateAsyncScope();
        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();

        var graph = (await notes.GetGraphAsync(null, CancellationToken.None)).Value!;

        graph.Nodes.Should().HaveCount(3);
        // C(3,2) = 3 unique undirected edges
        graph.Edges.Should().HaveCount(3);
    }

    [Fact]
    public async Task Graph_Filters_By_Workspace()
    {
        using var fixture = new TestAppDbContext();
        var workspaces = await CreateWorkspacesAsync(fixture, "Arch", "Other");

        await CreateNoteAsync(fixture, workspaces[0], "N1", "tag");
        await CreateNoteAsync(fixture, workspaces[1], "N2", "tag");

        await using var scope = fixture.Provider.CreateAsyncScope();
        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();

        var filtered = (await notes.GetGraphAsync(workspaces[0], CancellationToken.None)).Value!;
        filtered.Nodes.Should().ContainSingle(n => n.Id.ToString() != Guid.Empty.ToString());
        filtered.Nodes.Should().HaveCount(1);
        filtered.Edges.Should().BeEmpty();
    }

    [Fact]
    public async Task Graph_Node_Contains_Workspace_And_Tags()
    {
        using var fixture = new TestAppDbContext();
        var workspace = (await CreateWorkspacesAsync(fixture, "Arch"))[0];
        await CreateNoteAsync(fixture, workspace, "One", "dotnet", "database");

        await using var scope = fixture.Provider.CreateAsyncScope();
        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();

        var node = (await notes.GetGraphAsync(null, CancellationToken.None)).Value!.Nodes.Single();

        node.WorkspaceName.Should().Be("Arch");
        node.Tags.Should().BeEquivalentTo("dotnet", "database");
    }

    [Fact]
    public async Task Graph_With_No_Notes_Is_Empty()
    {
        using var fixture = new TestAppDbContext();

        await using var scope = fixture.Provider.CreateAsyncScope();
        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();

        var graph = (await notes.GetGraphAsync(null, CancellationToken.None)).Value!;

        graph.Nodes.Should().BeEmpty();
        graph.Edges.Should().BeEmpty();
    }
}