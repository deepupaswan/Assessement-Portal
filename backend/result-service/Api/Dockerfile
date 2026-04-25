# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Dev.sln", "."]
COPY ["backend/result-service/Api/ResultService.Api.csproj", "backend/result-service/Api/"]
COPY ["backend/result-service/Application/ResultService.Application.csproj", "backend/result-service/Application/"]
COPY ["backend/result-service/Domain/ResultService.Domain.csproj", "backend/result-service/Domain/"]
COPY ["backend/result-service/Infrastructure/ResultService.Infrastructure.csproj", "backend/result-service/Infrastructure/"]

# Restore dependencies
RUN dotnet restore "backend/result-service/Api/ResultService.Api.csproj"

# Copy source code
COPY ["backend/result-service/", "backend/result-service/"]

# Build application
WORKDIR "/src/backend/result-service/Api"
RUN dotnet build "ResultService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ResultService.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 80 443

# Install tools needed for HTTPS and health checks
RUN apt-get update && apt-get install -y ca-certificates curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "ResultService.Api.dll"]
