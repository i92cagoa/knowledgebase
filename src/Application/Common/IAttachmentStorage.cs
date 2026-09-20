namespace KnowledgeBase.Application.Common;

public interface IAttachmentStorage
{
    Task<string> SaveAsync(
        Guid attachmentId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken);

    Task<Stream?> OpenAsync(string storagePath, CancellationToken cancellationToken);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken);
}