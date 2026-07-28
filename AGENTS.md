# AGENTS.md — MediaVoyager

ASP.NET Core 10 Web API backed by Azure Cosmos DB. A more detailed conventions doc lives at `.github/copilot-instructions.md` — read it before non-trivial changes.

## Build / Run

- From solution root: `dotnet build`, `dotnet run --project MediaVoyager/MediaVoyager.csproj`
- Dev URL is `http://localhost:5211` (per `launchSettings.json`) — README/DEPLOYMENT.md say 5000; that is stale.
- Health check: `GET /health`. Swagger UI is served in all environments, not just Development.
- `MediaVoyager.sln` has no test projects. `TMDbLib/TMDbLibTests` belongs to the vendored fork's own solution — do not wire it into the main build.

## Structure that isn't obvious

- Two-project solution: `MediaVoyager/` (the app) and `TMDbLib/` (vendored fork of the TMDbLib library, project-referenced). Treat `TMDbLib/` as third-party code — don't restyle or refactor it.
- `NewHorizonLib` is a NuGet package with no source in this repo. It provides `ICosmosDbService`, `ISecretService`, `ITokenService`, the `[RateLimit]` attribute, and `Registration.InitializeServices(...)`. Behavior inside it cannot be changed from here.
- Layering: Controllers → Services → Repositories → Cosmos. Each repository hardcodes its container name via `cosmosDbService.GetContainer("<name>")` (e.g. `UserMovies`, `movieCache`, `tvShowCache`).

## Deployment — trust the workflow, not DEPLOYMENT.md

- `.github/workflows/master_mediavoyager.yml` (on push to `master`) builds with .NET 10 and deploys to **Azure Web App** via `azure/webapps-deploy@v3`.
- `DEPLOYMENT.md`, `.deployment`, `project.toml`, `.buildpacks`, and `Dockerfile.disabled` describe an abandoned Container Apps/buildpack approach. They are stale — do not "fix" the app or workflow to match them.

## Conventions an agent is likely to break

- **Auth is custom**, not standard ASP.NET: protected endpoints read the user id from the `x-uid` header and validate with `tokenService.IsValidAuth(userId, HttpContext, GlobalConstant.Issuer)`, plus `[Authorize]` and `[RateLimit(requests, minutes)]`. Copy the exact pattern from copilot-instructions.md.
- **Routes**: most controllers use `[Route("api/[controller]")]`; `RecommendationController` uses `[Route("[controller]")]` (no `/api` prefix) — intentional. `DashboardController` and `ProviderController` are intentionally unauthenticated; `POST api/Provider/{provider}` switches the live AI provider at runtime.
- **JSON**: Newtonsoft.Json with a global snake_case naming strategy. Cosmos entities (`Entities/`) use lowercase property names (`id`, `favouriteMovies`); API models (`Models/`, `ApiRequest/`) use PascalCase. Do not normalize either side.
- **Secrets** are fetched at runtime via `ISecretService.GetSecretValue(...)`: `gemini_api_key`, `groq_api_key` (+ `groq_api_key_backup`), `tmdb_api_key` (v3 key for `TMDbClient`), `tmdb_auth` (v4 bearer token, assigned to `SecretUtility.tmdbAuthHeader` at startup), `omdb_api_key`. For local dev use `dotnet user-secrets set` (a `UserSecretsId` is set in the csproj).
- **DI**: everything is registered singleton except the scoped `IRequestLogCollector`; singleton `MediaRecommendationService` resolves it via `IHttpContextAccessor`. New services default to singleton.
- `<Nullable>enable</Nullable>` is on — keep new code null-safe.

## Core flow (recommendations)

`RecommendationController` → `MediaRecommendationService` (loads favorites + watch history from repos) → `RecommendationClientResolver` picks Gemini or Groq (`IRecommendationClient`, each client enforces its own per-minute/per-day rate limits) → AI's answer is searched on TMDb via `TmdbCacheService` (Cosmos-backed cache) → retries with higher temperature if the result was already watched → IMDb rating added via `OmdbClient`.

Background jobs use Quartz (see `WatchHistoryCleanupJob`, daily 03:00 UTC). Register new jobs in `Program.cs` with the same `AddJob`/`AddTrigger` pattern.
