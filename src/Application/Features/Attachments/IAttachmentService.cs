using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Application.Features.Attachments;

public interface IAttachmentService
{
    Task<Result<Guid>> UploadAsync(UploadAttachmentCommand command, CancellationToken cancellationToken);
    Task<Result<AttachmentContentResult>> GetContentAsync(Guid id, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(DeleteAttachmentCommand command, CancellationToken cancellationToken);
}