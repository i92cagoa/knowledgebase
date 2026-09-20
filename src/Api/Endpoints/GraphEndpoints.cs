using KnowledgeBase.Application.Features.Notes;

namespace KnowledgeBase.Api.Endpoints;

public static class GraphEndpoints
{
    public static IEndpointRouteBuilder MapGraphEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/graph", async (Guid? workspaceId, INoteService service, CancellationToken ct) =>
            (await service.GetGraphAsync(workspaceId, ct)).ToHttpResult())
            .WithTags("Graph");

        return app;
    }
}