namespace KnowledgeBase.Domain.Entities;

public sealed class NoteTag
{
    private NoteTag() { }

    public Guid NoteId { get; private set; }
    public Guid TagId { get; private set; }

    public Note Note { get; private set; } = null!;
    public Tag Tag { get; private set; } = null!;

    public static NoteTag Create(Guid noteId, Guid tagId)
    {
        return new NoteTag
        {
            NoteId = noteId,
            TagId = tagId
        };
    }
}