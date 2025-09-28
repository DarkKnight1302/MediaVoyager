# Azure Container Apps Deployment Guide for MediaVoyager

## Overview

This document explains the changes made to fix the Azure Container Apps deployment issue where the buildpack process was failing with "failed to pull run image" errors.

## Problem Statement

The original issue was:
- Buildpack deployment failing during the restore phase
- Error: "failed to pull run image" with retry attempts exceeded
- Unable to pull the run image `mcr.microsoft.com/oryx/builder@sha256:...`

## Solution Implemented

### 1. Docker Containerization

**Added Dockerfile** with multi-stage build:
- **Build Stage**: Uses `mcr.microsoft.com/dotnet/sdk:9.0` for building the application
- **Runtime Stage**: Uses `mcr.microsoft.com/dotnet/aspnet:9.0` for running the application
- **Security**: Non-root user (`appuser`) for enhanced security
- **Port Configuration**: Standardized on port 8080 for Azure Container Apps

### 2. Build Optimization

**Added .dockerignore** to exclude unnecessary files:
- Build artifacts (`bin/`, `obj/`)
- Development files (`.vs/`, `.vscode/`)
- Git files and documentation
- Logs and temporary files
- Node modules and package managers

### 3. Application Configuration

**Updated Program.cs** for container environments:
- Added health check endpoint at `/health`
- Configured console logging for containers
- Added graceful shutdown timeout (30 seconds)
- Conditional HTTPS redirection (disabled in production containers)
- Fixed null reference warnings

### 4. GitHub Actions Workflow

**Updated deployment workflow**:
- Replaced buildpack deployment with Docker container deployment
- Added `dockerfilePath: Dockerfile` parameter
- Updated to use `actions/checkout@v4`
- Removed buildpack-specific parameters

## Key Benefits

1. **Reliability**: Direct Docker build eliminates buildpack dependency issues
2. **Performance**: Multi-stage build reduces final image size
3. **Security**: Non-root user execution
4. **Monitoring**: Health check endpoint for container orchestration
5. **Maintainability**: Explicit configuration vs. black-box buildpacks

## Container Configuration

### Environment Variables
- `ASPNETCORE_URLS=http://+:8080`
- `ASPNETCORE_ENVIRONMENT=Production`

### Health Check
- **Endpoint**: `/health`
- **Interval**: 30 seconds
- **Timeout**: 10 seconds
- **Start Period**: 60 seconds
- **Retries**: 3

### Logging
- Console logging enabled for container environments
- Debug logging for development scenarios

## Deployment Process

1. **Build Stage**: 
   - Restore NuGet packages
   - Build the solution
   - Publish the MediaVoyager project

2. **Runtime Stage**:
   - Copy published application
   - Set up non-root user
   - Configure environment
   - Expose port 8080

3. **Health Monitoring**:
   - Container health checks via `/health` endpoint
   - Proper startup and shutdown handling

## Azure Container Apps Compatibility

The solution is specifically optimized for Azure Container Apps:
- Uses official Microsoft .NET container images
- Follows Azure Container Apps port conventions (8080)
- Includes health checks for container lifecycle management
- Handles secrets and configuration appropriately
- Supports graceful shutdown for container orchestration

## Testing the Solution

To test locally (if Docker environment supports it):
```bash
docker build -t mediavoyager:test .
docker run -p 8080:8080 mediavoyager:test
```

The application will be available at `http://localhost:8080` with health checks at `http://localhost:8080/health`.

## Next Steps

1. Deploy to Azure Container Apps using the updated GitHub Actions workflow
2. Monitor container health and performance
3. Verify all application functionality works in the containerized environment
4. Review logs for any container-specific issues

This solution provides a robust, maintainable approach to deploying the MediaVoyager application on Azure Container Apps while eliminating the buildpack-related deployment failures.