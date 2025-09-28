using MediaVoyager.Clients;
using MediaVoyager.Constants;
using MediaVoyager.Handlers;
using MediaVoyager.Repositories;
using MediaVoyager.Services;
using MediaVoyager.Services.Interfaces;
using NewHorizonLib;
using NewHorizonLib.Extensions;
using NewHorizonLib.Services;
using NewHorizonLib.Services.Interfaces;
using System.Net;
using TMDbLib.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

// Configure host options for containers
builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IGeminiRecommendationClient, GeminiRecommendationClient>();
builder.Services.AddSingleton<IUserMoviesRepository, UserMoviesRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IMediaRecommendationService, MediaRecommendationService>();
builder.Services.AddSingleton<IUserMediaService, UserMediaService>();
builder.Services.AddSingleton<ISignInHandler, SignInHandler>();
builder.Services.AddSingleton<IUserTvRepository, UserTvRepository>();

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

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
