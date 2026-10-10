using Microsoft.AspNetCore.Diagnostics;
using BuildingFlow.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Api;

public sealed class ExceptionHandler(ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken token)
    {
        var status = error switch { StorageUnavailableException => 503, DocumentSizeException => 413, RequestValidationException => 400, ArgumentException => 400, BadHttpRequestException request => request.StatusCode, UnauthorizedAccessException => 403, DbUpdateConcurrencyException or HierarchyConflictException => 409, _ => 500 };
        logger.LogError("Request failed with {ExceptionType} and status {StatusCode}", error.GetType().Name, status);
        context.Response.StatusCode = status;
        if (error is RequestValidationException validation)
        {
            var problem = new ValidationProblemDetails(validation.Errors) { Status = 400, Instance = context.Request.Path };
            ApiProblems.Apply(problem, ErrorCatalog.ValidationFailed.Create());
            problem.Extensions["fieldIssues"] = validation.Issues;
            await ApiProblems.Write(context, problem, token);
            return true;
        }
        var result = new ProblemDetails { Status = status, Instance = context.Request.Path };
        ApiProblems.Apply(result, error is HierarchyConflictException conflict ? conflict.Error : ApiProblems.ForStatus(status));
        await ApiProblems.Write(context, result, token);
        return true;
    }
}
