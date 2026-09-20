using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Domain.Entities;

public sealed class Note
{
    private Note() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string ContentMarkdown { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Guid WorkspaceId { get; private set; }
    public Workspace Workspace { get; private set; } = null!;

    public string? SourceUrl { get; private set; }
    public string? SourceSummary { get; private set; }

    public ICollection<NoteTag> NoteTags { get; private set; } = [];
    public ICollection<Attachment> Attachments { get; private set; } = [];

    public static Note Create(
        string title,
        string contentMarkdown,
        Guid workspaceId,
        string? sourceUrl = null,
        string? sourceSummary = null)
    {
        var now = DateTime.UtcNow;
        return new Note
        {
            Id = Guid.NewGuid(),
            Title = title,
            ContentMarkdown = contentMarkdown,
            WorkspaceId = workspaceId,
            SourceUrl = sourceUrl,
            SourceSummary = sourceSummary,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string title, string contentMarkdown, string? sourceUrl, string? sourceSummary)
    {
        Title = title;
        ContentMarkdown = contentMarkdown;
        SourceUrl = sourceUrl;
        SourceSummary = sourceSummary;
        UpdatedAt = DateTime.UtcNow;
    }
}