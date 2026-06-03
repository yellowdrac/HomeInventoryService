using HomeInventory.Application.Common.Results;

namespace HomeInventory.Api.Extensions;

/// <summary>Maps the application <see cref="Result"/> types to HTTP responses.</summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : Problem(result.Error);

    public static IResult ToHttpResult<TValue>(this Result<TValue> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error);

    private static IResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        };

        return Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Message);
    }
}
