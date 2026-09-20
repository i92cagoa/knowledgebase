using KnowledgeBase.Application.Features.Notes;

namespace KnowledgeBase.Api.Endpoints;

public static class NoteEndpoints
{
    public static IEndpointRouteBuilder MapNoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/notes")
            .WithTags("Notes");

        group.MapGet("/", async (
            string? titleQuery,
            string? tags,
            int? page,
            int? pageSize,
            INoteService service,
            CancellationToken ct) =>
        {
            var tagNames = string.IsNullOrWhiteSpace(tags)
                ? null
                : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var command = new SearchNotesCommand(
                titleQuery,
                tagNames,
                page ?? 1,
                pageSize ?? 20);

            return (await service.SearchAsync(command, ct)).ToHttpResult();
        });

        group.MapGet("/{id:guid}", async (Guid id, INoteService service, CancellationToken ct) =>
            (await service.GetByIdAsync(id, ct)).ToHttpResult());

        group.MapPut("/{id:guid}", async (Guid id, UpdateNoteRequest body, INoteService service, CancellationToken ct) =>
        {
            var command = new UpdateNoteCommand(
                id,
                body.Title,
                body.ContentMarkdown,
                body.Tags,
                body.SourceUrl,
                body.SourceSummary);

            return (await service.UpdateAsync(command, ct)).ToHttpResult();
        });

        group.MapDelete("/{id:guid}", async (Guid id, INoteService service, CancellationToken ct) =>
            (await service.DeleteAsync(new DeleteNoteCommand(id), ct)).ToHttpResult());

        return app;
    }
}