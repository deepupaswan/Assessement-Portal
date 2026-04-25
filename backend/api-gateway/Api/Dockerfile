# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Dev.sln", "."]
COPY ["backend/api-gateway/Api/ApiGateway.csproj", "backend/api-gateway/Api/"]

# Restore dependencies
RUN dotnet restore "backend/api-gateway/Api/ApiGateway.csproj"

# Copy source code
COPY ["backend/api-gateway/", "backend/api-gateway/"]

# Build application
WORKDIR "/src/backend/api-gateway/Api"
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish

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

ENTRYPOINT ["dotnet", "ApiGateway.dll"]
