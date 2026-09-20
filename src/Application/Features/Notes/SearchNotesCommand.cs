namespace KnowledgeBase.Application.Features.Notes;

public sealed record SearchNotesCommand(
    string? TitleQuery = null,
    IReadOnlyList<string>? TagNames = null,
    int Page = 1,
    int PageSize = 20);