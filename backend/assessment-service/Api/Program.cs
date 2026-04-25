using AssessmentService.Application.Services;
using AssessmentService.Infrastructure.Services;
using AssessmentService.Infrastructure.Persistence;
using AssessmentService.Api.Hubs;
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

// Add SignalR
builder.Services.AddSignalR();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AssessmentDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IAssessmentService, AssessmentService.Infrastructure.Services.AssessmentService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();

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
app.MapHub<AssessmentHub>("/hubs/assessment");
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "assessment-service" }));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AssessmentDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
