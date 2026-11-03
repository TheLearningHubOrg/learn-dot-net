using Microsoft.AspNetCore.Diagnostics;

namespace LearnDotNet.Api.Errors;

// Runs inside UseExceptionHandler(). Returning true marks the exception as
// handled; returning false passes it to the next registered handler.
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            BadHttpRequestException bad => (bad.StatusCode, "The request was malformed."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;

        // The exception message is deliberately left out of the response: it
        // can contain SQL, file paths or secrets. The trace id lets support
        // find the full stack trace in the logs.
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = status,
                Title = title,
                Extensions = { ["traceId"] = httpContext.TraceIdentifier },
            },
        });
    }
}
