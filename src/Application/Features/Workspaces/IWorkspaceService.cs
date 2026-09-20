using KnowledgeBase.Application.Features.Dtos;
using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Application.Features.Workspaces;

public interface IWorkspaceService
{
    Task<Result<Guid>> CreateAsync(CreateWorkspaceCommand command, CancellationToken cancellationToken);
    Task<Result> UpdateAsync(UpdateWorkspaceCommand command, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(DeleteWorkspaceCommand command, CancellationToken cancellationToken);
    Task<Result<WorkspaceDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<WorkspaceDto>>> ListAsync(CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<WorkspaceTreeDto>>> GetTreeAsync(CancellationToken cancellationToken);
}