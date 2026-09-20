using FluentValidation;
using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.Features.Dtos;
using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Application.Features.Notes;

public sealed class NoteService(
    IAppDbContext db,
    IValidator<CreateNoteCommand> createValidator,
    IValidator<UpdateNoteCommand> updateValidator) : INoteService
{
    public async Task<Result<Guid>> CreateAsync(CreateNoteCommand command, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Invalid(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var workspace = await db.Workspaces.FindAsync([command.WorkspaceId], cancellationToken);
        if (workspace is null)
        {
            return Error.NotFound($"Workspace '{command.WorkspaceId}' was not found.");
        }

        var note = Note.Create(
            command.Title,
            command.ContentMarkdown,
            command.WorkspaceId,
            command.SourceUrl,
            command.SourceSummary);

        await AssignTagsAsync(note, command.Tags, cancellationToken);

        db.Notes.Add(note);
        await db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(note.Id);
    }

    public async Task<Result> UpdateAsync(UpdateNoteCommand command, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Invalid(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var note = await db.Notes
            .Include(n => n.NoteTags)
            .ThenInclude(nt => nt.Tag)
            .SingleOrDefaultAsync(n => n.Id == command.Id, cancellationToken);
        if (note is null)
        {
            return Error.NotFound($"Note '{command.Id}' was not found.");
        }

        note.Update(command.Title, command.ContentMarkdown, command.SourceUrl, command.SourceSummary);

        await ReplaceTagsAsync(note, command.Tags, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(DeleteNoteCommand command, CancellationToken cancellationToken)
    {
        var note = await db.Notes.FindAsync([command.Id], cancellationToken);
        if (note is null)
        {
            return Error.NotFound($"Note '{command.Id}' was not found.");
        }

        db.Notes.Remove(note);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<NoteDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var note = await db.Notes
            .AsNoTracking()
            .AsSplitQuery()
            .Where(n => n.Id == id)
            .Select(n => new NoteDto(
                n.Id,
                n.Title,
                n.ContentMarkdown,
                n.CreatedAt,
                n.UpdatedAt,
                n.WorkspaceId,
                n.SourceUrl,
                n.SourceSummary,
                n.NoteTags.Select(nt => new TagDto(nt.Tag.Id, nt.Tag.Name, nt.Tag.Color)).ToList(),
                n.Attachments.Select(a => new AttachmentDto(
                    a.Id,
                    a.FileName,
                    a.ContentType,
                    a.Kind,
                    $"api/attachments/{a.Id}")).ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return note is null
            ? Error.NotFound($"Note '{id}' was not found.")
            : Result<NoteDto>.Success(note);
    }

    public async Task<Result<IReadOnlyList<NoteSummaryDto>>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var workspaceExists = await db.Workspaces.AnyAsync(w => w.Id == workspaceId, cancellationToken);
        if (!workspaceExists)
        {
            return Error.NotFound($"Workspace '{workspaceId}' was not found.");
        }

        var notes = await db.Notes
            .AsNoTracking()
            .Where(n => n.WorkspaceId == workspaceId)
            .OrderByDescending(n => n.UpdatedAt)
            .Select(n => new NoteSummaryDto(
                n.Id,
                n.Title,
                n.UpdatedAt,
                n.NoteTags.Select(nt => nt.Tag.Name).ToList()))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<NoteSummaryDto>>.Success(ToReadOnlyList(notes));
    }

    public async Task<Result<PagedResult<NoteSearchResultDto>>> SearchAsync(SearchNotesCommand command, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, command.Page);
        var pageSize = Math.Clamp(command.PageSize, 1, 100);

        var hasTitleQuery = !string.IsNullOrWhiteSpace(command.TitleQuery);
        var titleQuery = command.TitleQuery?.Trim() ?? string.Empty;
        var tagNames = command.TagNames?
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList() ?? [];

        var query = db.Notes
            .AsNoTracking()
            .AsSplitQuery();

        if (hasTitleQuery)
        {
            var pattern = $"%{titleQuery}%";
            query = query.Where(n =>
                EF.Functions.Like(n.Title, pattern) ||
                EF.Functions.Like(n.ContentMarkdown, pattern));
        }

        if (tagNames.Count > 0)
        {
            query = query.Where(n => n.NoteTags.Any(nt => tagNames.Contains(nt.Tag.Name)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NoteSearchResultDto(
                n.Id,
                n.Title,
                n.UpdatedAt,
                n.WorkspaceId,
                n.Workspace.Name,
                n.NoteTags.Select(nt => nt.Tag.Name).ToList()))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        var result = new PagedResult<NoteSearchResultDto>(ToReadOnlyList(items), page, pageSize, totalCount, totalPages);
        return Result<PagedResult<NoteSearchResultDto>>.Success(result);
    }

    public async Task<Result<GraphDto>> GetGraphAsync(Guid? workspaceId, CancellationToken cancellationToken)
    {
        var query = db.Notes
            .AsNoTracking()
            .AsSplitQuery();

        if (workspaceId is not null)
        {
            query = query.Where(n => n.WorkspaceId == workspaceId);
        }

        var notes = await query
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.WorkspaceId,
                WorkspaceName = n.Workspace.Name,
                Tags = n.NoteTags.Select(nt => nt.Tag.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        var nodes = notes
            .Select(n => new GraphNodeDto(
                n.Id,
                n.Title,
                n.WorkspaceId,
                n.WorkspaceName,
                n.Tags))
            .ToList();

        var noteTuples = notes
            .Select(n => (n.Id, n.Title, n.WorkspaceId, n.WorkspaceName, n.Tags))
            .ToList();

        var edges = BuildSharedTagEdges(noteTuples);

        return Result<GraphDto>.Success(new GraphDto(ToReadOnlyList(nodes), ToReadOnlyList(edges)));
    }

    private static List<GraphEdgeDto> BuildSharedTagEdges(
        IReadOnlyList<(Guid Id, string Title, Guid WorkspaceId, string WorkspaceName, List<string> Tags)> notes)
    {
        var byTag = new Dictionary<string, List<Guid>>();
        foreach (var note in notes)
        {
            foreach (var tag in note.Tags)
            {
                if (!byTag.TryGetValue(tag, out var list))
                {
                    list = [];
                    byTag[tag] = list;
                }

                list.Add(note.Id);
            }
        }

        var seen = new HashSet<(Guid, Guid)>();
        var edges = new List<GraphEdgeDto>();

        foreach (var group in byTag.Values)
        {
            for (var i = 0; i < group.Count; i++)
            {
                for (var j = i + 1; j < group.Count; j++)
                {
                    var a = group[i];
                    var b = group[j];
                    var key = a.CompareTo(b) < 0 ? (a, b) : (b, a);
                    if (seen.Add(key))
                    {
                        edges.Add(new GraphEdgeDto(key.Item1, key.Item2));
                    }
                }
            }
        }

        return edges;
    }

    private async Task AssignTagsAsync(Note note, IReadOnlyList<string> tagNames, CancellationToken cancellationToken)
    {
        var distinctNames = tagNames
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();

        if (distinctNames.Count == 0)
        {
            return;
        }

        var existing = await db.Tags
            .Where(t => distinctNames.Contains(t.Name))
            .ToListAsync(cancellationToken);

        foreach (var name in distinctNames)
        {
            var tag = existing.FirstOrDefault(t => t.Name == name) ?? Tag.Create(name);
            note.NoteTags.Add(NoteTag.Create(note.Id, tag.Id));

            if (!existing.Contains(tag))
            {
                existing.Add(tag);
                db.Tags.Add(tag);
            }
        }
    }

    private async Task ReplaceTagsAsync(Note note, IReadOnlyList<string> tagNames, CancellationToken cancellationToken)
    {
        var distinctNames = tagNames
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();

        var kept = new HashSet<Guid>();
        var tracked = note.NoteTags.Select(nt => nt.TagId).ToHashSet();

        foreach (var name in distinctNames)
        {
            var tag = note.NoteTags
                .Select(nt => nt.Tag)
                .FirstOrDefault(t => t.Name == name);

            if (tag is null)
            {
                tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
                if (tag is null)
                {
                    tag = Tag.Create(name);
                    db.Tags.Add(tag);
                }
            }

            kept.Add(tag.Id);
            if (!tracked.Contains(tag.Id))
            {
                db.NoteTags.Add(NoteTag.Create(note.Id, tag.Id));
            }
        }

        foreach (var nt in note.NoteTags.Where(nt => !kept.Contains(nt.TagId)).ToList())
        {
            db.NoteTags.Remove(nt);
        }
    }

    private static IReadOnlyList<T> ToReadOnlyList<T>(List<T> list) => list;
}