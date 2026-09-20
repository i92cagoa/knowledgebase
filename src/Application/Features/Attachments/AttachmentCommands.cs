using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Features.Attachments;

public sealed record UploadAttachmentCommand(
    Guid NoteId,
    string FileName,
    string ContentType,
    AttachmentKind Kind,
    Stream Content);

public sealed record DeleteAttachmentCommand(Guid Id);

public sealed record AttachmentContentResult(string ContentType, string FileName, Stream Content);