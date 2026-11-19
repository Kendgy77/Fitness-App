using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FitnessApp.Api.Common.Errors;

/// <summary>
/// Turns every unhandled exception into an RFC 7807 ProblemDetails response. Only domain exceptions carry a
/// message meant for the user; anything else becomes a generic 500 so no internals leak to the client.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetails) : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // The client gave up (closed the tab, timed out): nothing to report and nobody to answer.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        var (status, title) = exception switch
        {
            DomainValidationException => (StatusCodes.Status400BadRequest, exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Wystąpił nieoczekiwany błąd."),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        // A rule about one field gets the same shape as attribute validation: { errors: { field: [message] } }.
        ProblemDetails body = exception is DomainValidationException { Field: { } field }
            ? new HttpValidationProblemDetails(new Dictionary<string, string[]> { [field] = [exception.Message] })
            : new ProblemDetails();
        body.Status = status;
        body.Title = title;

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = body,
            Exception = exception,
        });
    }
}
