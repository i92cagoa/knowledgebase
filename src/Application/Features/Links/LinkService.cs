using FluentValidation;
using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.Features.Notes;
using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Application.Features.Links;

public sealed class LinkService(
    IAppDbContext db,
    ILinkContentFetcher fetcher,
    ILinkAnalyzer analyzer,
    INoteService noteService,
    IValidator<ImportLinkCommand> validator) : ILinkService
{
    public async Task<Result<Guid>> ImportAsync(Guid workspaceId, ImportLinkCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Invalid(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var workspace = await db.Workspaces.FindAsync([workspaceId], cancellationToken);
        if (workspace is null)
        {
            return Error.NotFound($"Workspace '{workspaceId}' was not found.");
        }

        if (!Uri.TryCreate(command.Url, UriKind.Absolute, out var url) ||
            (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
        {
            return Error.Invalid("Url must be an absolute http(s) address.");
        }

        FetchedLink fetched;
        try
        {
            fetched = await fetcher.FetchAsync(url, cancellationToken);
        }
        catch (Exception ex)
        {
            return Error.Invalid($"Could not fetch the link: {ex.Message}");
        }

        LinkAnalysis analysis;
        try
        {
            analysis = await analyzer.AnalyzeAsync(fetched, cancellationToken);
        }
        catch (Exception ex)
        {
            return Error.Invalid($"Could not analyze the link: {ex.Message}");
        }

        var title = string.IsNullOrWhiteSpace(analysis.Title) ? fetched.Title : analysis.Title;
        var markdown = BuildMarkdown(analysis, url, fetched);

        var noteResult = await noteService.CreateAsync(
            new CreateNoteCommand(
                workspaceId,
                title,
                markdown,
                analysis.Tags,
                url.ToString(),
                analysis.Summary),
            cancellationToken);

        return noteResult;
    }

    private static string BuildMarkdown(LinkAnalysis analysis, Uri url, FetchedLink fetched)
    {
        var summary = string.IsNullOrWhiteSpace(analysis.Summary) ? fetched.Excerpt : analysis.Summary;
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"# {analysis.Title ?? fetched.Title}");
        if (summary is { Length: > 0 })
        {
            builder.AppendLine();
            builder.AppendLine(summary);
        }
        builder.AppendLine();
        builder.AppendLine($"Source: [{url.Host}]({url})");
        return builder.ToString();
    }
}

public sealed class ImportLinkCommandValidator : AbstractValidator<ImportLinkCommand>
{
    public ImportLinkCommandValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out var u) &&
                         (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Url must be an absolute http(s) address.");
    }
}