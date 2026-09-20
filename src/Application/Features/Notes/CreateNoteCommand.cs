namespace KnowledgeBase.Application.Features.Notes;

public sealed record CreateNoteCommand(
    Guid WorkspaceId,
    string Title,
    string ContentMarkdown,
    IReadOnlyList<string> Tags,
    string? SourceUrl = null,
    string? SourceSummary = null);