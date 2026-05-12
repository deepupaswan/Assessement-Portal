using IdentityService.Api.Controllers;
using IdentityService.Api.Middleware;
using MassTransit;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Core;
using Swashbuckle.AspNetCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging (CRITICAL FOR DEBUGGING)
builder.Host.UseSerilog((hostContext, loggerConfig) =>
{
    var isDevelopment = hostContext.HostingEnvironment.IsDevelopment();
    
    loggerConfig
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("MachineName", Environment.MachineName)
        .Enrich.WithProperty("Service", "IdentityService")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/identity-service-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] [{UserEmail}] {Message:lj}{NewLine}{Exception}");

    if (isDevelopment)
    {
        loggerConfig.MinimumLevel.Debug();
    }
});

// Load user secrets in development (CRITICAL SECURITY)
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add FluentValidation for input validation (CRITICAL SECURITY)
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add Rate Limiting for authentication endpoints (CRITICAL SECURITY)
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    // Per-user rate limit: 5 attempts per 15 minutes on login
    rateLimiterOptions.AddFixedWindowLimiter("login", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(15);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Per-IP rate limit: 20 attempts per 15 minutes
    rateLimiterOptions.AddFixedWindowLimiter("ip-based", options =>
    {
        options.PermitLimit = 20;
        options.Window = TimeSpan.FromMinutes(15);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Rejection behavior
    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    rateLimiterOptions.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            message = "Too many requests. Please try again later.",
            retryAfter = context.HttpContext.Response.Headers.RetryAfter
        });
    };
});

// Add CORS configuration for frontend and docker containers
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
        ?? new[] { "http://localhost:4200", "http://localhost:55942", "http://frontend", "http://frontend:80" };
    
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Configure MassTransit with RabbitMQ for cross-service domain events.
var rabbitmqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitmqPort = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672");
var rabbitmqUser = builder.Configuration["RabbitMQ:Username"]
    ?? throw new InvalidOperationException("RabbitMQ:Username is required.");
var rabbitmqPassword = builder.Configuration["RabbitMQ:Password"]
    ?? throw new InvalidOperationException("RabbitMQ:Password is required.");

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri($"rabbitmq://{rabbitmqHost}:{rabbitmqPort}"), h =>
        {
            h.Username(rabbitmqUser);
            h.Password(rabbitmqPassword);
        });
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddInfrastructureServices(connectionString);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    dbContext.Database.Migrate();
}

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// Enable rate limiting BEFORE other middleware
app.UseRateLimiter();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors("AllowFrontend");
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "identity-service" }));
app.Run();
