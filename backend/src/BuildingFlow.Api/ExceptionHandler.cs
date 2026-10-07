using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Api;

public sealed class ExceptionHandler(IProblemDetailsService problems, ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken token)
    {
        var status = error switch { ArgumentException => 400, DbUpdateConcurrencyException => 409, _ => 500 };
        logger.LogError("Request failed with {ExceptionType} and status {StatusCode}", error.GetType().Name, status);
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = new ProblemDetails { Status = status, Title = status == 500 ? "Unexpected server error" : status == 409 ? "Concurrent update conflict" : "Invalid request" } });
    }
}
