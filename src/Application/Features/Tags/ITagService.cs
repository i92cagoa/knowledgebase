using KnowledgeBase.Application.Features.Dtos;
using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Application.Features.Tags;

public interface ITagService
{
    Task<Result<Guid>> CreateAsync(CreateTagCommand command, CancellationToken cancellationToken);
    Task<Result> UpdateAsync(UpdateTagCommand command, CancellationToken cancellationToken);
    Task<Result> MergeAsync(MergeTagCommand command, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(DeleteTagCommand command, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<TagItemDto>>> ListAsync(CancellationToken cancellationToken);
}