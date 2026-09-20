namespace KnowledgeBase.Domain.Entities;

public sealed class Workspace
{
    private Workspace() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ICollection<Note> Notes { get; private set; } = [];

    public static Workspace Create(string name, string? description)
    {
        var now = DateTime.UtcNow;
        return new Workspace
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}