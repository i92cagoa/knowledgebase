namespace KnowledgeBase.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new HealthResponse("healthy", DateTime.UtcNow)))
            .WithName("GetHealth")
            .WithTags("Health");

        return app;
    }

    public sealed record HealthResponse(string Status, DateTime Timestamp);
}