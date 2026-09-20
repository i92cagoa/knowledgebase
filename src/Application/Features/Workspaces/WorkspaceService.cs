using FluentValidation;
using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.Features.Dtos;
using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Application.Features.Workspaces;

public sealed class WorkspaceService(
    IAppDbContext db,
    IValidator<CreateWorkspaceCommand> createValidator,
    IValidator<UpdateWorkspaceCommand> updateValidator) : IWorkspaceService
{
    public async Task<Result<Guid>> CreateAsync(CreateWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Invalid(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var exists = await db.Workspaces
            .AnyAsync(w => w.Name == command.Name, cancellationToken);
        if (exists)
        {
            return Error.Conflict($"A workspace named '{command.Name}' already exists.");
        }

        var workspace = Workspace.Create(command.Name, command.Description);

        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(workspace.Id);
    }

    public async Task<Result> UpdateAsync(UpdateWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Invalid(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var workspace = await db.Workspaces.FindAsync([command.Id], cancellationToken);
        if (workspace is null)
        {
            return Error.NotFound($"Workspace '{command.Id}' was not found.");
        }

        var duplicated = await db.Workspaces
            .AnyAsync(w => w.Name == command.Name && w.Id != command.Id, cancellationToken);
        if (duplicated)
        {
            return Error.Conflict($"A workspace named '{command.Name}' already exists.");
        }

        workspace.Update(command.Name, command.Description);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(DeleteWorkspaceCommand command, CancellationToken cancellationToken)
    {
        var workspace = await db.Workspaces.FindAsync([command.Id], cancellationToken);
        if (workspace is null)
        {
            return Error.NotFound($"Workspace '{command.Id}' was not found.");
        }

        db.Workspaces.Remove(workspace);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<WorkspaceDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var workspace = await db.Workspaces
            .AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new WorkspaceDto(w.Id, w.Name, w.Description, w.CreatedAt, w.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return workspace is null
            ? Error.NotFound($"Workspace '{id}' was not found.")
            : Result<WorkspaceDto>.Success(workspace);
    }

    public async Task<Result<IReadOnlyList<WorkspaceDto>>> ListAsync(CancellationToken cancellationToken)
    {
        var workspaces = await db.Workspaces
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .Select(w => new WorkspaceDto(w.Id, w.Name, w.Description, w.CreatedAt, w.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<WorkspaceDto>>.Success(ToReadOnlyList(workspaces));
    }

    public async Task<Result<IReadOnlyList<WorkspaceTreeDto>>> GetTreeAsync(CancellationToken cancellationToken)
    {
        var trees = await db.Workspaces
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .Select(w => new WorkspaceTreeDto(
                w.Id,
                w.Name,
                w.Notes
                    .OrderByDescending(n => n.UpdatedAt)
                    .Select(n => new NoteSummaryDto(
                        n.Id,
                        n.Title,
                        n.UpdatedAt,
                        n.NoteTags.Select(nt => nt.Tag.Name).ToList()))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<WorkspaceTreeDto>>.Success(ToReadOnlyList(trees));
    }

    private static IReadOnlyList<T> ToReadOnlyList<T>(List<T> list) => list;
}