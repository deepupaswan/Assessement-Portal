# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Dev.sln", "."]
COPY ["backend/identity-service/Api/IdentityService.Api.csproj", "backend/identity-service/Api/"]
COPY ["backend/identity-service/Application/IdentityService.Application.csproj", "backend/identity-service/Application/"]
COPY ["backend/identity-service/Domain/IdentityService.Domain.csproj", "backend/identity-service/Domain/"]
COPY ["backend/identity-service/Infrastructure/IdentityService.Infrastructure.csproj", "backend/identity-service/Infrastructure/"]

# Restore dependencies
RUN dotnet restore "backend/identity-service/Api/IdentityService.Api.csproj"

# Copy source code
COPY ["backend/identity-service/", "backend/identity-service/"]

# Build application
WORKDIR "/src/backend/identity-service/Api"
RUN dotnet build "IdentityService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "IdentityService.Api.csproj" -c Release -o /app/publish

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

ENTRYPOINT ["dotnet", "IdentityService.Api.dll"]
