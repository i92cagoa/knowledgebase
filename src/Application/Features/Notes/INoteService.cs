using KnowledgeBase.Application.Features.Dtos;
using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Application.Features.Notes;

public interface INoteService
{
    Task<Result<Guid>> CreateAsync(CreateNoteCommand command, CancellationToken cancellationToken);
    Task<Result> UpdateAsync(UpdateNoteCommand command, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(DeleteNoteCommand command, CancellationToken cancellationToken);
    Task<Result<NoteDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<NoteSummaryDto>>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken);
    Task<Result<PagedResult<NoteSearchResultDto>>> SearchAsync(SearchNotesCommand command, CancellationToken cancellationToken);
}