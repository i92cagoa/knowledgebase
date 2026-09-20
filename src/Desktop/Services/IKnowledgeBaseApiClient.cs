using KnowledgeBase.Desktop.Models;

namespace KnowledgeBase.Desktop.Services;

public interface IKnowledgeBaseApiClient
{
    Task<List<WorkspaceTree>?> GetTreeAsync();
    Task<List<Tag>?> GetTagsAsync();
    Task<Guid> CreateTagAsync(string name, string color);
    Task UpdateTagAsync(Guid id, string name, string color);
    Task DeleteTagAsync(Guid id);
    Task MergeTagAsync(Guid sourceTagId, Guid targetTagId);
    Task<Note?> GetNoteAsync(Guid id);
    Task<PagedSearchResult<NoteSearchItem>?> SearchNotesAsync(string query, string? tags, int page, int pageSize);
    Task<GraphData?> GetGraphAsync(Guid? workspaceId = null);
    Task<List<Workspace>?> GetWorkspacesAsync();
    Task<Guid> CreateWorkspaceAsync(string name, string? description);
    Task DeleteWorkspaceAsync(Guid id);
    Task<Guid> CreateNoteAsync(Guid workspaceId, CreateNoteRequest request);
    Task<Guid> ImportLinkAsync(Guid workspaceId, string url);
    Task UpdateNoteAsync(Guid id, UpdateNoteRequest request);
    Task DeleteNoteAsync(Guid id);
    Task<Guid> UploadAttachmentAsync(Guid noteId, string filePath, bool isCanvas);
    Task DeleteAttachmentAsync(Guid id);
}