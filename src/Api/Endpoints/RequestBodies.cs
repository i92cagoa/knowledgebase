namespace KnowledgeBase.Api.Endpoints;

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