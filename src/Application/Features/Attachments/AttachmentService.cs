using FluentValidation;
using KnowledgeBase.Application.Common;
using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Features.Attachments;

public sealed class AttachmentService(
    IAppDbContext db,
    IAttachmentStorage storage,
    IValidator<UploadAttachmentCommand> uploadValidator) : IAttachmentService
{
    public async Task<Result<Guid>> UploadAsync(UploadAttachmentCommand command, CancellationToken cancellationToken)
    {
        var validation = await uploadValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Invalid(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var note = await db.Notes.FindAsync([command.NoteId], cancellationToken);
        if (note is null)
        {
            return Error.NotFound($"Note '{command.NoteId}' was not found.");
        }

        var attachment = Attachment.Create(
            command.FileName,
            command.ContentType,
            storagePath: string.Empty,
            command.Kind,
            command.NoteId);

        db.Attachments.Add(attachment);
        await db.SaveChangesAsync(cancellationToken);

        var storagePath = await storage.SaveAsync(
            attachment.Id,
            command.FileName,
            command.Content,
            cancellationToken);

        attachment.SetStoragePath(storagePath);
        await db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(attachment.Id);
    }

    public async Task<Result<AttachmentContentResult>> GetContentAsync(Guid id, CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments.FindAsync([id], cancellationToken);
        if (attachment is null)
        {
            return Error.NotFound($"Attachment '{id}' was not found.");
        }

        var stream = await storage.OpenAsync(attachment.StoragePath, cancellationToken);
        if (stream is null)
        {
            return Error.NotFound($"Content for attachment '{id}' was not found.");
        }

        return new AttachmentContentResult(attachment.ContentType, attachment.FileName, stream);
    }

    public async Task<Result> DeleteAsync(DeleteAttachmentCommand command, CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments.FindAsync([command.Id], cancellationToken);
        if (attachment is null)
        {
            return Error.NotFound($"Attachment '{command.Id}' was not found.");
        }

        db.Attachments.Remove(attachment);
        await storage.DeleteAsync(attachment.StoragePath, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}