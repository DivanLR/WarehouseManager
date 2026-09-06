using System.Diagnostics;
using Microsoft.Extensions.Primitives;

namespace WarehouseManager.Api.Behaviors;

internal sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = ResolveCorrelationId(context);

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

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out StringValues supplied) &&
            !string.IsNullOrWhiteSpace(supplied))
        {
            return supplied.ToString();
        }

        return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }
}
