var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "http://localhost:4300", "http://localhost:55942")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.MapGet("/", () => Results.Ok(new
{
    service = "api-gateway",
    status = "ok"
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy"
}));

app.MapReverseProxy();

app.Run();
