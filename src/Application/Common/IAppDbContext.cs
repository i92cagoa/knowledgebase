using KnowledgeBase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Application.Common;

public interface IAppDbContext
{
    DbSet<Workspace> Workspaces { get; }
    DbSet<Note> Notes { get; }
    DbSet<Tag> Tags { get; }
    DbSet<NoteTag> NoteTags { get; }
    DbSet<Attachment> Attachments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}