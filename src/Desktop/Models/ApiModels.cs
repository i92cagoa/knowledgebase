namespace KnowledgeBase.Desktop.Models;

public sealed record Workspace(Guid Id, string Name, string? Description, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record Tag(Guid Id, string Name, string Color, int NoteCount);
public sealed record NoteSummary(Guid Id, string Title, DateTime UpdatedAt, IReadOnlyList<string> Tags);
public sealed record WorkspaceTree(Guid Id, string Name, IReadOnlyList<NoteSummary> Notes);

public sealed record Note(
    Guid Id,
    string Title,
    string ContentMarkdown,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid WorkspaceId,
    string? SourceUrl,
    string? SourceSummary,
    IReadOnlyList<Tag> Tags,
    IReadOnlyList<Attachment> Attachments);

public sealed record Attachment(Guid Id, string FileName, string ContentType, int Kind, string Url);

public sealed record NoteSearchItem(
    Guid Id,
    string Title,
    DateTime UpdatedAt,
    Guid WorkspaceId,
    string WorkspaceName,
    IReadOnlyList<string> Tags);

public sealed record PagedSearchResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CreateWorkspaceRequest(string Name, string? Description);
public sealed record UpdateWorkspaceRequest(string Name, string? Description);

public sealed record CreateTagRequest(string Name, string Color);
public sealed record UpdateTagRequest(string Name, string Color);
public sealed record MergeTagsRequest(Guid SourceTagId, Guid TargetTagId);

public sealed record CreateNoteRequest(
    string Title,
    string ContentMarkdown,
    IReadOnlyList<string> Tags,
    string? SourceUrl = null,
    string? SourceSummary = null);

public sealed record UpdateNoteRequest(
    string Title,
    string ContentMarkdown,
    IReadOnlyList<string> Tags,
    string? SourceUrl = null,
    string? SourceSummary = null);