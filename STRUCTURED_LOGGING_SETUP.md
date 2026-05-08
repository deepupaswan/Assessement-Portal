# Structured Logging Implementation with Correlation IDs

## Overview
Implemented **Serilog** for structured logging across ALL backend services with automatic correlation ID injection. This enables request tracing across multiple microservices and provides production-ready observability.

**Status**: ✅ **IMPLEMENTED** (All 5 API services + 1 correlation ID middleware per service)

## What Was Added

### 1. **NuGet Packages**
```xml
<PackageReference Include="Serilog" Version="4.0.0" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.1" />
<PackageReference Include="Serilog.Enrichers.CorrelationId" Version="3.0.1" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
```

**Package Purpose**:
- `Serilog`: Structured logging framework
- `Serilog.AspNetCore`: ASP.NET Core integration
- `Serilog.Enrichers.CorrelationId`: Automatic correlation ID enrichment
- `Serilog.Sinks.Console`: Log to console with formatting
- `Serilog.Sinks.File`: Persistent file logging with rolling intervals

### 2. **Serilog Configuration in Program.cs**

Added to all 5 services:
- `backend/assessment-service/Api/Program.cs`
- `backend/identity-service/Api/Program.cs`
- `backend/answer-service/Api/Program.cs`
- `backend/candidate-service/Api/Program.cs`
- `backend/result-service/Api/Program.cs`

**Configuration Pattern**:
```csharp
builder.Host.UseSerilog((hostContext, loggerConfig) =>
{
    var isDevelopment = hostContext.HostingEnvironment.IsDevelopment();
    
    loggerConfig
        .MinimumLevel.Information()
        .Enrich.FromLogContext()           // Capture LogContext properties
        .Enrich.WithMachineName()           // Add machine name
        .Enrich.WithProperty("Service", "AssessmentService")  // Service identifier
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/assessment-service-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] [{UserEmail}] {Message:lj}{NewLine}{Exception}");

    if (isDevelopment)
    {
        loggerConfig.MinimumLevel.Debug();  // More verbose in development
    }
});
```

### 3. **Correlation ID Middleware**

Created for each service:
- `backend/assessment-service/Api/Middleware/CorrelationIdMiddleware.cs`
- `backend/identity-service/Api/Middleware/CorrelationIdMiddleware.cs`
- `backend/answer-service/Api/Middleware/CorrelationIdMiddleware.cs`
- `backend/candidate-service/Api/Middleware/CorrelationIdMiddleware.cs`
- `backend/result-service/Api/Middleware/CorrelationIdMiddleware.cs`

**What It Does**:
```csharp
public async Task InvokeAsync(HttpContext context)
{
    // 1. Extract or generate correlation ID from request header
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
        ?? context.TraceIdentifier;

    // 2. Add correlation ID to response header
    context.Response.Headers.Add("X-Correlation-ID", correlationId);

    // 3. Inject into LogContext for all logs in this request
    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        // 4. Extract and inject user email from JWT
        var userEmail = context.User.FindFirst("email")?.Value ?? "anonymous";
        using (LogContext.PushProperty("UserEmail", userEmail))
        {
            // 5. Extract IP address for security auditing
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            using (LogContext.PushProperty("IpAddress", ipAddress))
            {
                // 6. Log request metadata
                using (LogContext.PushProperty("RequestMethod", context.Request.Method))
                {
                    using (LogContext.PushProperty("RequestPath", context.Request.Path))
                    {
                        await _next(context);  // Call next middleware
                    }
                }
            }
        }
    }
}
```

### 4. **Middleware Registration**

Added to all services' Program.cs after GlobalExceptionMiddleware:
```csharp
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();  // NEW: Injects correlation ID
app.UseCors("AllowFrontend");
```

## Log Output Examples

### Console Output (Development)
```
[14:23:45 INF] [AssessmentService] [550e8400-e29b-41d4-a716-446655440000] User logged in successfully
[14:23:46 DBG] [AssessmentService] [550e8400-e29b-41d4-a716-446655440000] GetAllAssessmentsAsync called with pageNumber=1
[14:23:47 INF] [AssessmentService] [550e8400-e29b-41d4-a716-446655440000] Found 25 assessments
[14:23:48 ERR] [AssessmentService] [550e8400-e29b-41d4-a716-446655440000] Failed to update assessment
  System.InvalidOperationException: Assessment not found
```

### File Output (logs/assessment-service-20250428.log)
```
2025-04-28 14:23:45.123 +00:00 [INF] [550e8400-e29b-41d4-a716-446655440000] [user@test.com] User logged in successfully
2025-04-28 14:23:46.456 +00:00 [DBG] [550e8400-e29b-41d4-a716-446655440000] [user@test.com] GetAllAssessmentsAsync called with pageNumber=1
2025-04-28 14:23:47.789 +00:00 [INF] [550e8400-e29b-41d4-a716-446655440000] [user@test.com] Found 25 assessments
2025-04-28 14:23:48.012 +00:00 [ERR] [550e8400-e29b-41d4-a716-446655440000] [user@test.com] Failed to update assessment
  System.InvalidOperationException: Assessment not found
```

## Using Structured Logging in Code

### Example 1: Simple Logging
```csharp
public class AssessmentsController : ControllerBase
{
    private readonly ILogger<AssessmentsController> _logger;

    public AssessmentsController(ILogger<AssessmentsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAssessments(int pageNumber = 1)
    {
        _logger.LogInformation("Fetching assessments for page {PageNumber}", pageNumber);
        
        var result = await _assessmentService.GetAllAssessmentsAsync(pageNumber, 20);
        
        _logger.LogInformation("Found {Count} assessments", result.Items.Count);
        return Ok(result.Items);
    }
}
```

**Output**:
```
[14:23:45 INF] [AssessmentService] [550e8400...] Fetching assessments for page 1
[14:23:46 INF] [AssessmentService] [550e8400...] Found 25 assessments
```

### Example 2: Logging with Context
```csharp
[HttpPost]
[Authorize]
public async Task<IActionResult> CreateAssessment([FromBody] CreateAssessmentRequest request)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    // Additional context is automatically included by middleware
    _logger.LogInformation("Creating assessment '{Title}' by user {UserId}", 
        request.Title, userId);
    
    try
    {
        var assessment = await _assessmentService.CreateAssessmentAsync(
            request.Title, request.Description, request.DurationMinutes, request.RandomizeQuestions);
        
        _logger.LogInformation("Assessment '{Title}' created with ID {AssessmentId}", 
            request.Title, assessment.Id);
        
        return CreatedAtAction(nameof(GetAssessmentDetails), 
            new { id = assessment.Id }, assessment);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create assessment '{Title}'", request.Title);
        throw;
    }
}
```

**Output**:
```
[14:23:45 INF] [AssessmentService] [550e8400...] [user@test.com] [192.168.1.100] POST /api/assessments
  Creating assessment 'Math Quiz' by user user@test.com
[14:23:46 INF] [AssessmentService] [550e8400...] [user@test.com] 
  Assessment 'Math Quiz' created with ID 12345678-1234-1234-1234-123456789012
```

## Distributed Tracing Across Services

When one service calls another, pass the correlation ID header:

```csharp
public class AssessmentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AssessmentService> _logger;

    public async Task<CandidateData> GetCandidateDataAsync(Guid candidateId)
    {
        var correlationId = LogContext.GetProperty("CorrelationId");
        
        var request = new HttpRequestMessage(HttpMethod.Get, 
            $"http://candidate-service/api/candidates/{candidateId}");
        
        // Pass correlation ID to downstream service
        request.Headers.Add("X-Correlation-ID", correlationId?.ToString());
        
        var response = await _httpClient.SendAsync(request);
        
        _logger.LogInformation("Retrieved candidate data for {CandidateId}", candidateId);
        
        return await response.Content.ReadAsAsync<CandidateData>();
    }
}
```

**Result**: Single correlation ID traces the entire request flow across all services.

## Log File Management

### File Rolling
Logs automatically roll to new files daily:
```
logs/
  assessment-service-20250428.log     (today's logs)
  assessment-service-20250427.log     (yesterday's logs)
  assessment-service-20250426.log     (older logs)
```

### Old Log Cleanup
Currently keeps all historical logs. For production, add retention:
```csharp
.WriteTo.File(
    path: "logs/assessment-service-.log",
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 30)  // Keep last 30 days
```

## Log Levels

| Level | When to Use | Example |
|-------|-----------|---------|
| Debug | Development troubleshooting | `_logger.LogDebug("Query result: {@Result}", result)` |
| Information | Important business events | `_logger.LogInformation("User {Email} logged in", email)` |
| Warning | Recoverable issues | `_logger.LogWarning("Retry attempt {Attempt} for query", attempt)` |
| Error | Error with context | `_logger.LogError(ex, "Failed to save assessment")` |
| Critical | System failure | `_logger.LogCritical("Database connection lost")` |

## Properties Automatically Logged

Every log automatically includes:
- **CorrelationId**: Trace request across services
- **UserEmail**: Which user made the request (if authenticated)
- **IpAddress**: Client IP for security auditing
- **RequestMethod**: HTTP verb (GET, POST, etc.)
- **RequestPath**: URL path accessed
- **Timestamp**: When the request occurred
- **MachineName**: Which server processed the request
- **Service**: Service name (e.g., "AssessmentService")

## Query Logs in Production

Example: Find all logs for a specific correlation ID:
```bash
grep "550e8400-e29b-41d4-a716-446655440000" logs/*.log
```

Example: Find all errors in the last hour:
```bash
tail -f logs/assessment-service-*.log | grep "\[ERR\]"
```

## Next Steps

1. **Elasticsearch Integration**: Ship logs to Elasticsearch for centralized querying
2. **Log Alerting**: Set up alerts for ERROR and CRITICAL logs
3. **Application Insights**: Azure monitoring for performance metrics
4. **Custom Enrichers**: Add business context (TenantId, FeatureName, etc.)

## Reference Documentation
- [Serilog Documentation](https://serilog.net/)
- [Serilog for ASP.NET Core](https://github.com/serilog/serilog-aspnetcore)
- [Structured Logging Best Practices](https://github.com/serilog/serilog/wiki)
