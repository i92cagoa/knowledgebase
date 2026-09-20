using FluentValidation;
using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.Features.Dtos;
using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Application.Features.Tags;

public sealed class TagService(
    IAppDbContext db,
    IValidator<CreateTagCommand> createValidator) : ITagService
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

    public async Task<Result<IReadOnlyList<TagDto>>> ListAsync(CancellationToken cancellationToken)
    {
        var tags = await db.Tags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name, t.Color))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<TagDto>>.Success(ToReadOnlyList(tags));
    }

    private static IReadOnlyList<T> ToReadOnlyList<T>(List<T> list) => list;
}