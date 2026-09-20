using KnowledgeBase.Desktop.Models;
using KnowledgeBase.Desktop.Services;

namespace KnowledgeBase.DesktopTests;

public sealed class FakeKnowledgeBaseApiClient : IKnowledgeBaseApiClient
{
    public Guid DefaultWorkspaceId { get; } = Guid.NewGuid();
    public Guid DefaultNoteId { get; } = Guid.NewGuid();
    public Guid DefaultTagId { get; } = Guid.NewGuid();

    public List<WorkspaceTree> Tree { get; } = [];
    public List<Tag> Tags { get; } = [];
    public Note? Note { get; set; }
    public GraphData? Graph { get; set; }
    public List<Workspace> Workspaces { get; } = [];

    public int CreateWorkspaceCalls { get; private set; }
    public int DeleteWorkspaceCalls { get; private set; }
    public int CreateNoteCalls { get; private set; }
    public int UpdateNoteCalls { get; private set; }
    public int DeleteNoteCalls { get; private set; }
    public int CreateTagCalls { get; private set; }
    public int UpdateTagCalls { get; private set; }
    public int DeleteTagCalls { get; private set; }
    public int MergeTagCalls { get; private set; }
    public int ImportLinkCalls { get; private set; }
    public Guid LastNoteId { get; private set; }

    public FakeKnowledgeBaseApiClient()
    {
        Tree.Add(new WorkspaceTree(DefaultWorkspaceId, "Dev", [new NoteSummary(DefaultNoteId, "First note", DateTime.UtcNow, ["dotnet"])]));
        Tags.Add(new Tag(DefaultTagId, "dotnet", "#512BD4", 1));
        Note = new Note(DefaultNoteId, "First note", "# body", DateTime.UtcNow, DateTime.UtcNow, DefaultWorkspaceId, null, null, [new Tag(DefaultTagId, "dotnet", "#512BD4", 1)], []);
        Graph = new GraphData(
            [new GraphNode(DefaultNoteId, "First note", DefaultWorkspaceId, "Dev", ["dotnet"])],
            []);
        Workspaces.Add(new Workspace(DefaultWorkspaceId, "Dev", null, DateTime.UtcNow, DateTime.UtcNow));
    }

    public Task<List<WorkspaceTree>?> GetTreeAsync() => Task.FromResult<List<WorkspaceTree>?>(Tree);

    public Task<List<Tag>?> GetTagsAsync() => Task.FromResult<List<Tag>?>(Tags);

    public Task<Guid> CreateTagAsync(string name, string color)
    {
        CreateTagCalls++;
        var id = Guid.NewGuid();
        Tags.Add(new Tag(id, name, color, 0));
        return Task.FromResult(id);
    }

    public Task UpdateTagAsync(Guid id, string name, string color)
    {
        UpdateTagCalls++;
        var tag = Tags.First(t => t.Id == id);
        Tags.Remove(tag);
        Tags.Add(tag with { Name = name, Color = color });
        return Task.CompletedTask;
    }

    public Task DeleteTagAsync(Guid id)
    {
        DeleteTagCalls++;
        Tags.RemoveAll(t => t.Id == id);
        return Task.CompletedTask;
    }

    public Task MergeTagAsync(Guid sourceTagId, Guid targetTagId)
    {
        MergeTagCalls++;
        var source = Tags.FirstOrDefault(t => t.Id == sourceTagId);
        if (source is not null)
        {
            Tags.Remove(source);
        }

        return Task.CompletedTask;
    }

    public Task<Note?> GetNoteAsync(Guid id) => Task.FromResult<Note?>(Note);

    public Task<PagedSearchResult<NoteSearchItem>?> SearchNotesAsync(string query, string? tags, int page, int pageSize)
        => Task.FromResult<PagedSearchResult<NoteSearchItem>?>(new PagedSearchResult<NoteSearchItem>(
            [new NoteSearchItem(DefaultNoteId, Note?.Title ?? "result", DateTime.UtcNow, DefaultWorkspaceId, "Dev", ["dotnet"])],
            1,
            pageSize,
            1,
            1));

    public Task<GraphData?> GetGraphAsync(Guid? workspaceId = null) => Task.FromResult<GraphData?>(Graph);

    public Task<List<Workspace>?> GetWorkspacesAsync() => Task.FromResult<List<Workspace>?>(Workspaces);

    public Task<Guid> CreateWorkspaceAsync(string name, string? description)
    {
        CreateWorkspaceCalls++;
        var id = Guid.NewGuid();
        Workspaces.Add(new Workspace(id, name, description, DateTime.UtcNow, DateTime.UtcNow));
        Tree.Add(new WorkspaceTree(id, name, []));
        return Task.FromResult(id);
    }

    public Task DeleteWorkspaceAsync(Guid id)
    {
        DeleteWorkspaceCalls++;
        Workspaces.RemoveAll(w => w.Id == id);
        Tree.RemoveAll(w => w.Id == id);
        return Task.CompletedTask;
    }

    public Task<Guid> CreateNoteAsync(Guid workspaceId, CreateNoteRequest request)
    {
        CreateNoteCalls++;
        LastNoteId = Guid.NewGuid();
        return Task.FromResult(LastNoteId);
    }

    public Task<Guid> ImportLinkAsync(Guid workspaceId, string url)
    {
        ImportLinkCalls++;
        return Task.FromResult(Guid.NewGuid());
    }

    public Task UpdateNoteAsync(Guid id, UpdateNoteRequest request)
    {
        UpdateNoteCalls++;
        return Task.CompletedTask;
    }

    public Task DeleteNoteAsync(Guid id)
    {
        DeleteNoteCalls++;
        return Task.CompletedTask;
    }

    public Task<Guid> UploadAttachmentAsync(Guid noteId, string filePath, bool isCanvas) =>
        Task.FromResult(Guid.NewGuid());

    public Task DeleteAttachmentAsync(Guid id) => Task.CompletedTask;
}