# Azure Container Apps Deployment Guide for MediaVoyager

## Overview

This guide explains the Azure Container Apps deployment configuration for the MediaVoyager application, using source-based deployment with Azure Container Apps built-in buildpacks.

## Problem Statement

The original Docker-based deployment configuration was failing due to:
1. **Azure Container Registry (ACR) Subscription Issues**: The Azure subscription is not registered for Microsoft.ContainerRegistry namespace
2. **ACR Not Available**: Azure Container Registry is not purchased or available in the subscription
3. **Complex Docker Configuration**: Using Docker added unnecessary complexity when Container Apps buildpacks can handle .NET applications directly

## Solution Implemented

### 1. Source-Based Deployment

**Removed Docker dependency**:
- **No Dockerfile**: Dockerfile renamed to `Dockerfile.disabled` to prevent Docker-based builds
- **Buildpack Deployment**: Uses Azure Container Apps built-in buildpacks for .NET applications
- **Simplified Process**: No external container registry required
- **Azure-Native**: Leverages Container Apps internal build capabilities

### 2. Application Configuration

**Updated Program.cs** for container environments:
- Added health check endpoint at `/health`
- Configured console logging for containers
- Added graceful shutdown timeout (30 seconds)
- Conditional HTTPS redirection (disabled in production containers)
- Fixed null reference warnings

### 3. GitHub Actions Workflow

**Updated deployment workflow**:
- **Removed ACR dependency**: No Azure Container Registry creation or management
- **Source-based deployment**: Uses `appSourcePath` with Azure Container Apps buildpacks
- **Simplified configuration**: Only requires app name, resource group, and source path
- **No registry parameters**: Removed all registry-related configurations
- Updated to use `actions/checkout@v4`

## Key Benefits

1. **No ACR Required**: Eliminates Azure Container Registry subscription dependency
2. **Simplified Deployment**: Uses Container Apps built-in buildpacks for .NET applications
3. **Cost Effective**: No external registry costs or management overhead
4. **Azure-Native**: Leverages platform capabilities without external dependencies
5. **Faster Deployment**: Direct source-to-container deployment
6. **Maintainability**: Reduced configuration complexity

## Container Configuration

### Environment Variables
- `ASPNETCORE_URLS=http://+:8080` (set by buildpack)
- `ASPNETCORE_ENVIRONMENT=Production` (set by buildpack)

### Health Check
- **Endpoint**: `/health`
- **Configured in**: Program.cs
- **Used by**: Container Apps for health monitoring

### Logging
- Console logging enabled for container environments
- Debug logging for development scenarios

## Deployment Process

1. **Source Upload**: GitHub Actions uploads source code to Azure Container Apps
2. **Buildpack Detection**: Container Apps detects .NET application and selects appropriate buildpack
3. **Build Process**: Buildpack handles dotnet restore, build, and publish automatically
4. **Container Creation**: Buildpack creates optimized container image internally
5. **Deployment**: Container is deployed to Azure Container Apps environment

## Azure Container Apps Compatibility

The solution is specifically optimized for Azure Container Apps:
- Uses Azure Container Apps built-in .NET buildpacks
- Follows Container Apps port conventions (buildpack sets port 8080)
- Includes health checks for container lifecycle management
- Handles secrets and configuration appropriately
- Supports graceful shutdown for container orchestration

## Testing the Solution

To test locally with .NET:
```bash
cd MediaVoyager
dotnet restore
dotnet build
dotnet run
```

The application will be available at `http://localhost:5000` (or configured port) with health checks at `/health`.

## Next Steps

1. **Deploy to Azure Container Apps using the updated GitHub Actions workflow**
   - Workflow now uses source-based deployment without ACR
   - No container registry setup required
   - Buildpacks handle all build and containerization automatically
2. Monitor container health and performance
3. Verify all application functionality works in the containerized environment
4. Review logs for any container-specific issues

## Buildpack Approach

This solution follows the same approach as TrafficEscape2.0, which successfully deploys without ACR:
- No Dockerfile required
- Azure Container Apps buildpacks handle .NET application detection
- Automatic build and containerization
- No external registry dependencies
- Simplified GitHub Actions workflow

This approach eliminates the Microsoft.ContainerRegistry namespace registration requirement and provides a more maintainable deployment solution.