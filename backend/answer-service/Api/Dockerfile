# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Dev.sln", "."]
COPY ["backend/answer-service/Api/AnswerService.Api.csproj", "backend/answer-service/Api/"]
COPY ["backend/answer-service/Application/AnswerService.Application.csproj", "backend/answer-service/Application/"]
COPY ["backend/answer-service/Domain/AnswerService.Domain.csproj", "backend/answer-service/Domain/"]
COPY ["backend/answer-service/Infrastructure/AnswerService.Infrastructure.csproj", "backend/answer-service/Infrastructure/"]

# Restore dependencies
RUN dotnet restore "backend/answer-service/Api/AnswerService.Api.csproj"

# Copy source code
COPY ["backend/answer-service/", "backend/answer-service/"]

# Build application
WORKDIR "/src/backend/answer-service/Api"
RUN dotnet build "AnswerService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AnswerService.Api.csproj" -c Release -o /app/publish

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

ENTRYPOINT ["dotnet", "AnswerService.Api.dll"]
