using BuildingFlow.Application;
using Microsoft.AspNetCore.Mvc;

namespace BuildingFlow.Api;

internal static class ApiProblems
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        // Match ASP.NET's UTF-8 JSON response behavior; this body is never embedded in HTML.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    public static ApiErrorMessage ForStatus(int status) => (status switch
    {
        400 => ErrorCatalog.InvalidRequest,
        401 => ErrorCatalog.SignInRequired,
        403 => ErrorCatalog.AccessDenied,
        404 => ErrorCatalog.ResourceNotFound,
        405 => ErrorCatalog.MethodNotAllowed,
        409 => ErrorCatalog.RecordChanged,
        413 => ErrorCatalog.DocumentTooLarge,
        503 => ErrorCatalog.StorageUnavailable,
        415 => ErrorCatalog.UnsupportedMedia,
        429 => ErrorCatalog.RateLimited,
        _ => ErrorCatalog.UnexpectedFailure
    }).Create();

    public static void Apply(ProblemDetails problem, ApiErrorMessage error)
    {
        problem.Title = error.Message;
        problem.Extensions["code"] = error.Code;
        problem.Extensions["message"] = error.Message;
        problem.Extensions["parameters"] = error.Parameters;
        problem.Extensions["severity"] = error.Severity;
    }

    public static void Customize(ProblemDetailsContext context)
    {
        // Framework binding failures and empty status responses use the same public contract.
        if (!context.ProblemDetails.Extensions.ContainsKey("code"))
            Apply(context.ProblemDetails, ForStatus(context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode));
        context.ProblemDetails.Detail = null;
        context.ProblemDetails.Instance = context.HttpContext.Request.Path;
    }

    public static Task Write(HttpContext context, ProblemDetails problem, CancellationToken token) =>
        problem is ValidationProblemDetails validation
            ? context.Response.WriteAsJsonAsync(validation, options: Json, contentType: "application/problem+json", cancellationToken: token)
            : context.Response.WriteAsJsonAsync(problem, options: Json, contentType: "application/problem+json", cancellationToken: token);
}
