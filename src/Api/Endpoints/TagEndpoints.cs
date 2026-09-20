using KnowledgeBase.Application.Features.Tags;

namespace KnowledgeBase.Api.Endpoints;

public static class TagEndpoints
{
    public static IEndpointRouteBuilder MapTagEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/tags")
            .WithTags("Tags");

        group.MapGet("/", async (ITagService service, CancellationToken ct) =>
            (await service.ListAsync(ct)).ToHttpResult());

        group.MapPost("/", async (CreateTagCommand command, ITagService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/tags/{result.Value}", result.Value)
                : result.ToHttpResult();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ITagService service, CancellationToken ct) =>
            (await service.DeleteAsync(new DeleteTagCommand(id), ct)).ToHttpResult());

        return app;
    }
}