using CandidateService.Application.Services;
using CandidateService.Infrastructure.Services;
using CandidateService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddScoped<ICandidateService, CandidateService.Infrastructure.Services.CandidateService>();
builder.Services.AddScoped<ICandidateAssessmentService, CandidateAssessmentService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");
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
