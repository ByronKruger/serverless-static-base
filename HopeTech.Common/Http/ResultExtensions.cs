using HopeTech.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace HopeTech.Common.Http;

/// <summary>
/// The one place a <see cref="Result"/> becomes HTTP. Controllers translate, they
/// do not decide: every error category has exactly one status code here.
/// </summary>
public static class ResultExtensions
{
    /// <summary>200 with the value, or the mapped problem response.</summary>
    public static ActionResult<TValue> ToActionResult<TValue>(this Result<TValue> result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.Ok(result.Value)
            : result.Error.ToProblem(controller);

    /// <summary>204, or the mapped problem response.</summary>
    public static ActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.NoContent()
            : result.Error.ToProblem(controller);

    /// <summary>201 with a Location header, or the mapped problem response.</summary>
    public static ActionResult<TValue> ToCreatedResult<TValue>(
        this Result<TValue> result,
        ControllerBase controller,
        string actionName,
        object routeValues) =>
        result.IsSuccess
            ? controller.CreatedAtAction(actionName, routeValues, result.Value)
            : result.Error.ToProblem(controller);

    private static ActionResult ToProblem(this Error error, ControllerBase controller)
    {
        ObjectResult result;

        if (error.Type is ErrorType.Validation && error.Failures is { Count: > 0 })
        {
            result = (ObjectResult)controller.ValidationProblem(new ValidationProblemDetails(
                error.Failures.ToDictionary(failure => failure.Key, failure => failure.Value))
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest
            });
        }
        else
        {
            var (status, title) = error.Type switch
            {
                ErrorType.Validation => (StatusCodes.Status400BadRequest, "One or more validation errors occurred."),
                ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Authentication is required."),
                ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "This action is not allowed."),
                ErrorType.NotFound => (StatusCodes.Status404NotFound, "The resource was not found."),
                ErrorType.Conflict => (StatusCodes.Status409Conflict, "The request conflicts with the current state."),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            result = controller.Problem(detail: error.Description, statusCode: status, title: title);
        }

        // The stable code is what a client branches on; the title is for humans.
        if (result.Value is ProblemDetails problemDetails)
        {
            problemDetails.Extensions["code"] = error.Code;
        }

        return result.AsProblemJson();
    }

    /// <summary>
    /// Forces the RFC 9457 media type. Neither <c>Problem()</c> nor
    /// <c>ValidationProblem()</c> sets it, so without this an error body is served as
    /// <c>application/json</c> and a client cannot tell a payload from a problem.
    /// </summary>
    internal static ObjectResult AsProblemJson(this ObjectResult result)
    {
        result.ContentTypes.Clear();
        result.ContentTypes.Add("application/problem+json");

        return result;
    }
}
