namespace KnowledgeBase.Domain.Entities;

public sealed class Tag
{
    private Tag() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public ICollection<NoteTag> NoteTags { get; private set; } = [];

    public static Tag Create(string name, string? color = null)
    {
        return new Tag
        {
            Id = Guid.NewGuid(),
            Name = name,
            Color = color ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
    }
}