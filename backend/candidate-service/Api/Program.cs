using CandidateService.Api.Middleware;
using CandidateService.Api.Events;
using CandidateService.Application.Services;
using CandidateService.Application.Repositories;
using CandidateService.Infrastructure.Services;
using CandidateService.Infrastructure.Persistence;
using CandidateService.Infrastructure.Persistence.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Core;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging (CRITICAL FOR DEBUGGING)
builder.Host.UseSerilog((hostContext, loggerConfig) =>
{
    var isDevelopment = hostContext.HostingEnvironment.IsDevelopment();
    
    loggerConfig
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("MachineName", Environment.MachineName)
        .Enrich.WithProperty("Service", "CandidateService")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/candidate-service-.log",
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

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add FluentValidation for input validation (CRITICAL SECURITY)
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<CandidateService.Infrastructure.Persistence.CandidateDbContext>(options =>
    options.UseSqlServer(connectionString));

var elasticsearchUrl = builder.Configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
builder.Services.AddHttpClient<ICandidateSearchService, CandidateSearchService>(client =>
{
    client.BaseAddress = new Uri(elasticsearchUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<ICandidateService, CandidateService.Infrastructure.Services.CandidateService>();
builder.Services.AddScoped<ICandidateRepository, CandidateRepository>();
builder.Services.AddScoped<ICandidateAssessmentService, CandidateAssessmentService>();
builder.Services.AddHttpContextAccessor();

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

builder.Services.AddHttpClient("InternalServices");

// Configure MassTransit with RabbitMQ for domain event publishing
var rabbitmqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitmqPort = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672");
var rabbitmqUser = builder.Configuration["RabbitMQ:Username"]
    ?? throw new InvalidOperationException("RabbitMQ:Username is required.");
var rabbitmqPassword = builder.Configuration["RabbitMQ:Password"]
    ?? throw new InvalidOperationException("RabbitMQ:Password is required.");

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CandidateRegisteredConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri($"rabbitmq://{rabbitmqHost}:{rabbitmqPort}"), h =>
        {
            h.Username(rabbitmqUser);
            h.Password(rabbitmqPassword);
        });

        cfg.ReceiveEndpoint("candidate-registered", e =>
        {
            e.ConfigureConsumer<CandidateRegisteredConsumer>(context);
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "candidate-service" }));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CandidateService.Infrastructure.Persistence.CandidateDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
