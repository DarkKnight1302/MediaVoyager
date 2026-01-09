using MediaVoyager.Clients;
using MediaVoyager.Constants;
using MediaVoyager.Handlers;
using MediaVoyager.Repositories;
using MediaVoyager.Services;
using MediaVoyager.Services.Interfaces;
using MediaVoyager.Middleware;
using NewHorizonLib;
using NewHorizonLib.Extensions;
using NewHorizonLib.Services;
using TMDbLib.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Configure logging for containers
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        // Configure property naming policy to handle underscores
        options.SerializerSettings.ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        };
        
        // Add TMDbLib converters
        options.SerializerSettings.Converters.Add(new TMDbLib.Utilities.Converters.TolerantEnumConverter());
        
        // Configure date handling if needed
        options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();

// Configure host options for containers
builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IGeminiRecommendationClient, GeminiRecommendationClient>();
builder.Services.AddSingleton<IGroqRecommendationClient, GroqRecommendationClient>();
builder.Services.AddSingleton<IOmdbClient, OmdbClient>();
builder.Services.AddSingleton<MediaVoyager.Services.Interfaces.IRecommendationClientResolver, RecommendationClientResolver>();
builder.Services.AddSingleton<IRecommendationProviderService, RecommendationProviderService>();
builder.Services.AddSingleton<IUserMoviesRepository, UserMoviesRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IUserMediaService, UserMediaService>();
builder.Services.AddSingleton<ISignInHandler, SignInHandler>();
builder.Services.AddSingleton<IUserTvRepository, UserTvRepository>();

// Cache repositories and services
builder.Services.AddSingleton<ICacheRepository<MediaVoyager.Entities.MovieCache>, MovieCacheRepository>();
builder.Services.AddSingleton<ICacheRepository<MediaVoyager.Entities.TvShowCache>, TvShowCacheRepository>();
builder.Services.AddSingleton<ITmdbCacheService, TmdbCacheService>();

// History repositories
builder.Services.AddSingleton<IUserMovieHistoryRepository, UserMovieHistoryRepository>();
builder.Services.AddSingleton<IUserTvHistoryRepository, UserTvHistoryRepository>();

// Dashboard and activity tracking services
builder.Services.AddSingleton<IUserActivityRepository, UserActivityRepository>();
builder.Services.AddSingleton<IDashboardService, DashboardService>();

// Error notification service
builder.Services.AddSingleton<IErrorNotificationService, ErrorNotificationService>();

// Request-scoped log collector for capturing logs during HTTP requests
builder.Services.AddScoped<IRequestLogCollector, RequestLogCollector>();

// Scoped services that depend on request-scoped log collector
builder.Services.AddScoped<IMediaRecommendationService, MediaRecommendationService>();

Registration.InitializeServices(builder.Services, builder.Configuration, "MediaVoyager", 0, GlobalConstant.Issuer, "MediaVoyagerClient");
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseRateLimiting();
var secretService = app.Services.GetService<ISecretService>();
if (secretService != null)
{
    string tmdbAuth = secretService.GetSecretValue("tmdb_auth");
    SecretUtility.tmdbAuthHeader = tmdbAuth;
}

// Enable serving static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseSwagger();
app.UseSwaggerUI();

// Only use HTTPS redirection in Development or when not in a container
if (app.Environment.IsDevelopment() || !app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// Handle HttpRequestException with429 status and return429 to REST clients
app.UseHttpRequestExceptionHandler();

app.MapControllers();
app.MapHealthChecks("/health");

// Map dashboard redirect
app.MapGet("/dashboard", context =>
{
    context.Response.Redirect("/dashboard/index.html");
    return Task.CompletedTask;
});

app.Run();
