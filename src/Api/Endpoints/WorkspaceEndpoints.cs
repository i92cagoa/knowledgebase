using KnowledgeBase.Application.Features.Notes;
using KnowledgeBase.Application.Features.Workspaces;

namespace KnowledgeBase.Api.Endpoints;

public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/workspaces")
            .WithTags("Workspaces");

        group.MapGet("/", async (string? embed, IWorkspaceService service, CancellationToken ct) =>
        {
            if (string.Equals(embed, "notes", StringComparison.OrdinalIgnoreCase))
            {
                var tree = await service.GetTreeAsync(ct);
                return tree.IsSuccess ? Results.Ok(tree.Value) : tree.ToHttpResult();
            }

            var list = await service.ListAsync(ct);
            return list.IsSuccess ? Results.Ok(list.Value) : list.ToHttpResult();
        });

        group.MapGet("/{id:guid}", async (Guid id, IWorkspaceService service, CancellationToken ct) =>
            (await service.GetByIdAsync(id, ct)).ToHttpResult());

        group.MapPost("/", async (CreateWorkspaceCommand command, IWorkspaceService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/workspaces/{result.Value}", result.Value)
                : result.ToHttpResult();
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateWorkspaceCommand body, IWorkspaceService service, CancellationToken ct) =>
        {
            var command = body with { Id = id };
            return (await service.UpdateAsync(command, ct)).ToHttpResult();
        });

        group.MapDelete("/{id:guid}", async (Guid id, IWorkspaceService service, CancellationToken ct) =>
            (await service.DeleteAsync(new DeleteWorkspaceCommand(id), ct)).ToHttpResult());

        group.MapGet("/{id:guid}/notes", async (Guid id, INoteService noteService, CancellationToken ct) =>
            (await noteService.ListByWorkspaceAsync(id, ct)).ToHttpResult());

        group.MapPost("/{id:guid}/notes", async (Guid id, CreateNoteRequest body, INoteService noteService, CancellationToken ct) =>
        {
            var command = new CreateNoteCommand(
                id,
                body.Title,
                body.ContentMarkdown,
                body.Tags,
                body.SourceUrl,
                body.SourceSummary);

            var result = await noteService.CreateAsync(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/notes/{result.Value}", result.Value)
                : result.ToHttpResult();
        });

        return app;
    }
}