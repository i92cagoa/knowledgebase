namespace KnowledgeBase.Application.Features.Notes;

public sealed record UpdateNoteCommand(
    Guid Id,
    string Title,
    string ContentMarkdown,
    IReadOnlyList<string> Tags,
    string? SourceUrl = null,
    string? SourceSummary = null);

public sealed record DeleteNoteCommand(Guid Id);