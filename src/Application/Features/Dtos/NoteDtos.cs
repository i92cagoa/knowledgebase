using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Features.Dtos;

public sealed record WorkspaceDto(Guid Id, string Name, string? Description, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record TagDto(Guid Id, string Name, string Color);

public sealed record AttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    AttachmentKind Kind,
    string Url);

public sealed record NoteDto(
    Guid Id,
    string Title,
    string ContentMarkdown,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid WorkspaceId,
    string? SourceUrl,
    string? SourceSummary,
    IReadOnlyList<TagDto> Tags,
    IReadOnlyList<AttachmentDto> Attachments);

public sealed record NoteSummaryDto(
    Guid Id,
    string Title,
    DateTime UpdatedAt,
    IReadOnlyList<string> Tags);

public sealed record WorkspaceTreeDto(
    Guid Id,
    string Name,
    IReadOnlyList<NoteSummaryDto> Notes);