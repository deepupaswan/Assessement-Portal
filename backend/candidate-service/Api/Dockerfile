# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Dev.sln", "."]
COPY ["backend/candidate-service/Api/CandidateService.Api.csproj", "backend/candidate-service/Api/"]
COPY ["backend/candidate-service/Application/CandidateService.Application.csproj", "backend/candidate-service/Application/"]
COPY ["backend/candidate-service/Domain/CandidateService.Domain.csproj", "backend/candidate-service/Domain/"]
COPY ["backend/candidate-service/Infrastructure/CandidateService.Infrastructure.csproj", "backend/candidate-service/Infrastructure/"]

# Restore dependencies
RUN dotnet restore "backend/candidate-service/Api/CandidateService.Api.csproj"

# Copy source code
COPY ["backend/candidate-service/", "backend/candidate-service/"]

# Build application
WORKDIR "/src/backend/candidate-service/Api"
RUN dotnet build "CandidateService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CandidateService.Api.csproj" -c Release -o /app/publish

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

ENTRYPOINT ["dotnet", "CandidateService.Api.dll"]
