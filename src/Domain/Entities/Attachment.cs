namespace KnowledgeBase.Domain.Entities;

public enum AttachmentKind
{
    Image = 1,
    Canvas = 2
}

public sealed class Attachment
{
    private Attachment() { }

    public Guid Id { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public AttachmentKind Kind { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Guid NoteId { get; private set; }
    public Note Note { get; private set; } = null!;

    public static Attachment Create(string fileName, string contentType, string storagePath, AttachmentKind kind, Guid noteId)
    {
        return new Attachment
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = contentType,
            StoragePath = storagePath,
            Kind = kind,
            NoteId = noteId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetStoragePath(string storagePath)
    {
        StoragePath = storagePath;
    }
}