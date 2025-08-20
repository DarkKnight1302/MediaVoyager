using MediaVoyager.Clients;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MediaVoyager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IGeminiRecommendationClient client;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IGeminiRecommendationClient geminiRecommendationClient)
        {
            _logger = logger;
            this.client = geminiRecommendationClient;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var favoriteMovies = new List<string> { "Swades", "Lagaan", "Ship of Theseus", "Rockstar", "3 idiots" };
            var watchHistory = new List<string> { "Gangs of Wasseypur", "Mirzapur", "Once Upon a Time in Mumbaai", "Black Friday", "Maqbool", "Vaastav", "Rang De Basanti", "Taare Zameen Par", "Zindagi Na Milegi Dobara", "Dangal" };

            _logger.LogInformation("--- Testing Gemini Recommendation Client with Rate Limiting ---");
            _logger.LogInformation($"Attempting to send 4 requests concurrently. Rate limit is 2 per minute.");
            _logger.LogInformation($"Start Time: {DateTime.Now:HH:mm:ss}\n");

            var stopwatch = Stopwatch.StartNew();

            // Create 4 concurrent tasks to simulate high load and test the rate limiter.
            var tasks = new List<Task<string>>
        {
            client.GetMovieRecommendationAsync(favoriteMovies, watchHistory),
            client.GetMovieRecommendationAsync(new List<string> { "The Dark Knight", "Inception", "The Prestige" }, watchHistory),
            client.GetMovieRecommendationAsync(new List<string> { "Pulp Fiction", "Forrest Gump", "The Shawshank Redemption" }, watchHistory),
            client.GetMovieRecommendationAsync(new List<string> { "Parasite", "Spirited Away", "Oldboy" }, watchHistory)
        };

            // Await all tasks and print results as they complete.
            var results = new List<string>();
            foreach (var task in tasks)
            {
                var result = await task;
                results.Add(result);
                _logger.LogInformation($"[{DateTime.Now:HH:mm:ss}] ✅ Recommendation Received: {result} (Elapsed: {stopwatch.Elapsed.TotalSeconds:F1}s)");
            }

            stopwatch.Stop();
            _logger.LogInformation($"\n--- Test Complete in {stopwatch.Elapsed.TotalSeconds:F1} seconds. ---");
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
