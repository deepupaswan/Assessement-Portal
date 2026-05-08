using Serilog.Context;

namespace ResultService.Api.Middleware;

/// <summary>
/// Middleware that injects correlation IDs for distributed tracing.
/// Correlation IDs track requests across multiple services in the system.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const string CorrelationIdProperty = "CorrelationId";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault() 
            ?? context.TraceIdentifier;

        if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
        {
            context.Response.Headers.Add(CorrelationIdHeader, correlationId);
        }

        using (LogContext.PushProperty(CorrelationIdProperty, correlationId))
        {
            var userEmail = context.User.FindFirst("email")?.Value ?? "anonymous";
            
            using (LogContext.PushProperty("UserEmail", userEmail))
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                
                using (LogContext.PushProperty("IpAddress", ipAddress))
                {
                    using (LogContext.PushProperty("RequestMethod", context.Request.Method))
                    {
                        using (LogContext.PushProperty("RequestPath", context.Request.Path))
                        {
                            await _next(context);
                        }
                    }
                }
            }
        }
    }
}
