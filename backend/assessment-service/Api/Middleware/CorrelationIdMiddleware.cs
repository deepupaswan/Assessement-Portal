using Serilog.Context;

namespace AssessmentService.Api.Middleware;

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

        // Add correlation ID to response headers so client can track the same request
        if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
        {
            context.Response.Headers.Add(CorrelationIdHeader, correlationId);
        }

        // Enrich all logs with correlation ID
        using (LogContext.PushProperty(CorrelationIdProperty, correlationId))
        {
            // Extract user email from JWT if authenticated
            var userEmail = context.User.FindFirst("email")?.Value ?? "anonymous";
            
            // Enrich with user context
            using (LogContext.PushProperty("UserEmail", userEmail))
            {
                // Extract IP address for security logging
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                
                using (LogContext.PushProperty("IpAddress", ipAddress))
                {
                    // Add request information
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
