using KnowledgeBase.Domain.Common;

namespace KnowledgeBase.Api.Endpoints;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return MapError(result.Error);
    }

    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return MapError(result.Error);
    }

    private static IResult MapError(Error error) => error.Code switch
    {
        "NotFound" => Results.NotFound(new ProblemDetailsBody(error.Code, error.Description)),
        "Conflict" => Results.Conflict(new ProblemDetailsBody(error.Code, error.Description)),
        "Invalid" => Results.BadRequest(new ProblemDetailsBody(error.Code, error.Description)),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: error.Description)
    };

    public sealed record ProblemDetailsBody(string Code, string Description);
}