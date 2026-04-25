var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
        ?? new[] { "http://localhost:4200", "http://localhost:4300", "http://localhost:55942", "http://frontend", "http://frontend:80" };
    
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

app.MapGet("/", () => Results.Ok(new
{
    service = "api-gateway",
    status = "ok"
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "api-gateway"
}));

app.MapReverseProxy();

app.Run();
