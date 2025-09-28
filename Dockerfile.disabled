# Multi-stage Dockerfile for MediaVoyager .NET 9.0 Web API

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY MediaVoyager.sln ./
COPY MediaVoyager/MediaVoyager.csproj MediaVoyager/
COPY TMDbLib/TMDbLib/TMDbLib.csproj TMDbLib/TMDbLib/

# Restore packages
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish MediaVoyager/MediaVoyager.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app
COPY --from=build /app/publish .

# Set permissions
RUN chown -R appuser:appuser /app
USER appuser

# Configure for container
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "MediaVoyager.dll"]