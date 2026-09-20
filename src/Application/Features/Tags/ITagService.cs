using KnowledgeBase.Application.Features.Dtos;
using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Application.Features.Tags;

public interface ITagService
{
    Task<Result<Guid>> CreateAsync(CreateTagCommand command, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(DeleteTagCommand command, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<TagDto>>> ListAsync(CancellationToken cancellationToken);
}