namespace LearnDotNet.Api.Middleware;

// Convention-based middleware: one instance for the app's lifetime, so it
// takes only singleton-safe dependencies in the constructor. Per-request
// services would go in InvokeAsync's parameter list instead.
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = IsAcceptable(incoming) ? incoming : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;

        // Headers must be set before the body starts streaming. OnStarting
        // runs just before the first byte is written, whichever code writes it.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    // Never trust a client header blindly: cap the length and the alphabet so
    // it cannot inject log lines or inflate header size.
    private static bool IsAcceptable(string value) =>
        value.Length is > 0 and <= 64 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
