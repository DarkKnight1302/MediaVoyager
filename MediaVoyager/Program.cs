using MediaVoyager.Clients;
using MediaVoyager.Constants;
using MediaVoyager.Repositories;
using MediaVoyager.Services;
using MediaVoyager.Services.Interfaces;
using NewHorizonLib;
using NewHorizonLib.Extensions;
using NewHorizonLib.Services;
using NewHorizonLib.Services.Interfaces;
using System.Net;
using TMDbLib.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IGeminiRecommendationClient, GeminiRecommendationClient>();
builder.Services.AddSingleton<IUserMoviesRepository, UserMoviesRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IMediaRecommendationService, MediaRecommendationService>();
builder.Services.AddSingleton<IUserMediaService, UserMediaService>();

Registration.InitializeServices(builder.Services, builder.Configuration, "MediaVoyager", 0, GlobalConstant.Issuer, "MediaVoyagerClient");
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseRateLimiting();
string tmdbAuth = app.Services.GetService<ISecretService>().GetSecretValue("tmdb_auth");
SecretUtility.tmdbAuthHeader = tmdbAuth;

app.UseAuthentication();
app.UseAuthorization();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapControllers();

app.Run();
