using FluentValidation;
using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.Features.Dtos;
using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Application.Features.Tags;

public sealed class TagService(
    IAppDbContext db,
    IValidator<CreateTagCommand> createValidator,
    IValidator<UpdateTagCommand> updateValidator,
    IValidator<MergeTagCommand> mergeValidator) : ITagService
{
    public async Task<Result<Guid>> CreateAsync(CreateTagCommand command, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Invalid(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var exists = await db.Tags.AnyAsync(t => t.Name == command.Name, cancellationToken);
        if (exists)
        {
            return Error.Conflict($"A tag named '{command.Name}' already exists.");
        }

        var tag = Tag.Create(command.Name, command.Color);

        db.Tags.Add(tag);
        await db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(tag.Id);
    }

    public async Task<Result> UpdateAsync(UpdateTagCommand command, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Invalid(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var tag = await db.Tags.FindAsync([command.Id], cancellationToken);
        if (tag is null)
        {
            return Error.NotFound($"Tag '{command.Id}' was not found.");
        }

        var duplicated = await db.Tags
            .AnyAsync(t => t.Name == command.Name && t.Id != command.Id, cancellationToken);
        if (duplicated)
        {
            return Error.Conflict($"A tag named '{command.Name}' already exists.");
        }

        tag.Update(command.Name, command.Color);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> MergeAsync(MergeTagCommand command, CancellationToken cancellationToken)
    {
        var validation = await mergeValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Invalid(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var source = await db.Tags.FindAsync([command.SourceTagId], cancellationToken);
        if (source is null)
        {
            return Error.NotFound($"Source tag '{command.SourceTagId}' was not found.");
        }

        var target = await db.Tags.FindAsync([command.TargetTagId], cancellationToken);
        if (target is null)
        {
            return Error.NotFound($"Target tag '{command.TargetTagId}' was not found.");
        }

        var links = await db.NoteTags
            .Where(nt => nt.TagId == source.Id)
            .ToListAsync(cancellationToken);

        foreach (var link in links)
        {
            var alreadyLinked = await db.NoteTags.AnyAsync(
                nt => nt.NoteId == link.NoteId && nt.TagId == target.Id,
                cancellationToken);

            if (!alreadyLinked)
            {
                db.NoteTags.Add(NoteTag.Create(link.NoteId, target.Id));
            }
        }

        db.NoteTags.RemoveRange(links);
        db.Tags.Remove(source);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(DeleteTagCommand command, CancellationToken cancellationToken)
    {
        var tag = await db.Tags.FindAsync([command.Id], cancellationToken);
        if (tag is null)
        {
            return Error.NotFound($"Tag '{command.Id}' was not found.");
        }

        db.Tags.Remove(tag);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<TagItemDto>>> ListAsync(CancellationToken cancellationToken)
    {
        var tags = await db.Tags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TagItemDto(
                t.Id,
                t.Name,
                t.Color,
                t.NoteTags.Count))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<TagItemDto>>.Success(ToReadOnlyList(tags));
    }

    private static IReadOnlyList<T> ToReadOnlyList<T>(List<T> list) => list;
}