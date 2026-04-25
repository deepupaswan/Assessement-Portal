# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Dev.sln", "."]
COPY ["backend/assessment-service/Api/AssessmentService.Api.csproj", "backend/assessment-service/Api/"]
COPY ["backend/assessment-service/Application/AssessmentService.Application.csproj", "backend/assessment-service/Application/"]
COPY ["backend/assessment-service/Domain/AssessmentService.Domain.csproj", "backend/assessment-service/Domain/"]
COPY ["backend/assessment-service/Infrastructure/AssessmentService.Infrastructure.csproj", "backend/assessment-service/Infrastructure/"]

# Restore dependencies
RUN dotnet restore "backend/assessment-service/Api/AssessmentService.Api.csproj"

# Copy source code
COPY ["backend/assessment-service/", "backend/assessment-service/"]

# Build application
WORKDIR "/src/backend/assessment-service/Api"
RUN dotnet build "AssessmentService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AssessmentService.Api.csproj" -c Release -o /app/publish

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

ENTRYPOINT ["dotnet", "AssessmentService.Api.dll"]
