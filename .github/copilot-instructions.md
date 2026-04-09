# Copilot Instructions — MediaVoyager

## Build & Run

```bash
# From solution root (where MediaVoyager.sln lives)
dotnet restore
dotnet build
dotnet run --project MediaVoyager/MediaVoyager.csproj

# Publish for deployment
dotnet publish MediaVoyager/MediaVoyager.csproj -c Release -o ./publish
```

The app runs on `http://localhost:5000` in development. Health check: `GET /health`.

There are no tests in this repository.

## Architecture

ASP.NET Core 10 Web API backed by Azure Cosmos DB, deployed to Azure Web App via GitHub Actions (`.github/workflows/master_mediavoyager.yml`). The solution has two projects: **MediaVoyager** (main app) and **TMDbLib** (forked TMDb API wrapper, project-referenced).

### Layer Overview

**Controllers → Services → Repositories → Cosmos DB**

- **Controllers** (`Controllers/`): Thin REST layer. Extract user ID from the `x-uid` request header, validate auth, delegate to services. Return `IActionResult`.
- **Services** (`Services/`): Business logic. Interfaces live in `Services/Interfaces/`.
- **Repositories** (`Repositories/`): Cosmos DB data access via `ICosmosDbService` from NewHorizonLib. Each repository calls `cosmosDbService.GetContainer("containerName")` for its container.
- **Clients** (`Clients/`): External API integrations (Gemini AI, Groq AI, OMDb). Both AI clients implement `IRecommendationClient` for runtime provider switching.
- **Entities** (`Entities/`): Cosmos DB documents — properties use **lowercase** (`id`, `userId`, `createdAt`).
- **Models** (`Models/`): API DTOs — properties use **PascalCase** (`Id`, `Title`, `ReleaseDate`).
- **Middleware** (`Middleware/`): Each middleware has an extension method for registration (e.g., `app.UseApiRequestLogging()`).

### Recommendation Pipeline

This is the core feature. The flow:

1. `RecommendationController` receives request
2. `MediaRecommendationService` orchestrates: fetches user favorites + watch history from repositories
3. `RecommendationClientResolver` picks the active AI provider (Gemini or Groq) based on `RecommendationProviderService` state
4. The selected `IRecommendationClient` calls the AI API with favorites, history, and a temperature parameter
5. The AI response (a movie/show name) is searched on TMDb via `TmdbCacheService` (which caches results in Cosmos DB)
6. If the result was already watched, the service retries with increased temperature
7. IMDb ratings are fetched via `OmdbClient` to enrich the response

### NewHorizonLib

Shared NuGet library providing cross-cutting concerns. Initialized in `Program.cs` via:
```csharp
Registration.InitializeServices(builder.Services, builder.Configuration, "MediaVoyager", 0, GlobalConstant.Issuer, "MediaVoyagerClient");
```

Key services it provides: `ICosmosDbService`, `ISecretService` (secret retrieval), `ITokenService` (JWT validation), `IOtpService`, `ResendEmailService`, and the `[RateLimit]` attribute.

## Conventions

### Authentication Pattern

Every protected endpoint follows this exact pattern:
```csharp
[Authorize]
[HttpPost("endpoint")]
[RateLimit(100, 5)]
public async Task<IActionResult> MyAction(...)
{
    string userId = HttpContext.Request.Headers["x-uid"].FirstOrDefault();
    if (string.IsNullOrEmpty(userId)) return BadRequest("User ID header is required");
    bool isValid = this.tokenService.IsValidAuth(userId, HttpContext, GlobalConstant.Issuer);
    if (!isValid) return Unauthorized();
    // ...
}
```

### DI Registrations

- **Singletons**: Clients, repositories, most services
- **Scoped**: `IRequestLogCollector` (one per HTTP request — collects timestamped logs for error reporting)
- `MediaRecommendationService` is registered as a singleton but resolves the scoped `IRequestLogCollector` at runtime via `IHttpContextAccessor`

### JSON Serialization

Newtonsoft.Json with **snake_case** naming strategy globally. TMDbLib's `TolerantEnumConverter` is registered for enum deserialization tolerance.

### Secrets

Retrieved at runtime via `ISecretService.GetSecretValue("key")`. Required secrets: `gemini_api_key`, `groq_api_key`, `tmdb_auth`, `omdb_api_key`.

### Rate Limiting

Two levels:
- **Controller-level**: `[RateLimit(requests, minutes)]` attribute from NewHorizonLib
- **Client-level**: AI clients implement their own per-minute and per-day rate limiting with `SemaphoreSlim` and request timestamp queues (Gemini: 9/min, 250/day; Groq: 15/min, 10000/day)

### Error Handling in Controllers

Controllers catch exceptions and send error notifications with request logs:
```csharp
catch (Exception ex)
{
    await errorNotificationService.SendErrorNotificationAsync(
        "GET /Recommendation/movie", userId, "Exception",
        $"Exception: {ex.Message}",
        requestLogCollector.GetLogs());
    return StatusCode(500, "...");
}
```

### Collections

User favorites, watchlists, and watch history use `HashSet<T>` for automatic deduplication. Model classes implement `IEquatable<T>` based on their ID.

### Route Prefixes

- Most controllers: `[Route("api/[controller]")]`
- `RecommendationController`: `[Route("[controller]")]` (no `/api` prefix)

### Activity Logging

User actions are tracked via `IUserActivityRepository.LogActivityAsync(userId, activityType, details)` for dashboard analytics.
