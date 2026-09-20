using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Application.Features.Links;

public sealed record FetchedLink(string Title, string Content, string? Excerpt);

public sealed record LinkAnalysis(string Title, string Summary, IReadOnlyList<string> Tags);

public sealed record ImportLinkCommand(string Url);

public interface ILinkContentFetcher
{
    Task<FetchedLink> FetchAsync(Uri url, CancellationToken cancellationToken);
}

public interface ILinkAnalyzer
{
    Task<LinkAnalysis> AnalyzeAsync(FetchedLink link, CancellationToken cancellationToken);
}

public interface ILinkService
{
    Task<Result<Guid>> ImportAsync(Guid workspaceId, ImportLinkCommand command, CancellationToken cancellationToken);
}