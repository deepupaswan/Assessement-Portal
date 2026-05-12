using AnswerService.Api.Middleware;
using AnswerService.Application.Services;
using AnswerService.Infrastructure;
using AnswerService.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Core;
using System.Text;
using Swashbuckle.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging (CRITICAL FOR DEBUGGING)
builder.Host.UseSerilog((hostContext, loggerConfig) =>
{
    var isDevelopment = hostContext.HostingEnvironment.IsDevelopment();
    
    loggerConfig
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("MachineName", Environment.MachineName)
        .Enrich.WithProperty("Service", "AnswerService")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/answer-service-.log",
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

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add FluentValidation for input validation (CRITICAL SECURITY)
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add CORS configuration for frontend and docker containers
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
        ?? new[] { "http://localhost:4200", "http://localhost:55942", "http://frontend" };
    
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddInfrastructureServices(connectionString);

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "IdentityService";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AssessmentPortal";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// Configure MassTransit with RabbitMQ
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
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AnswerDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "answer-service" }));

app.Run();
