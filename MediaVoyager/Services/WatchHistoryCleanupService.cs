using MediaVoyager.Clients;
using MediaVoyager.Entities;
using MediaVoyager.Models;
using MediaVoyager.Repositories;
using MediaVoyager.Services.Interfaces;

namespace MediaVoyager.Services
{
    public class WatchHistoryCleanupService : BackgroundService, IWatchHistoryCleanupService
    {
        private const int WatchHistoryThreshold = 130;
        private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

        private readonly IUserMoviesRepository userMoviesRepository;
        private readonly IUserMovieHistoryRepository userMovieHistoryRepository;
        private readonly IGroqRecommendationClient groqClient;
        private readonly ILogger<WatchHistoryCleanupService> logger;

        public WatchHistoryCleanupService(
            IUserMoviesRepository userMoviesRepository,
            IUserMovieHistoryRepository userMovieHistoryRepository,
            IGroqRecommendationClient groqClient,
            ILogger<WatchHistoryCleanupService> logger)
        {
            this.userMoviesRepository = userMoviesRepository;
            this.userMovieHistoryRepository = userMovieHistoryRepository;
            this.groqClient = groqClient;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait a bit after startup before running the first cleanup
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation("[WatchHistoryCleanup] Starting daily cleanup run");
                    await RunCleanupAsync(stoppingToken);
                    logger.LogInformation("[WatchHistoryCleanup] Daily cleanup run completed");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "[WatchHistoryCleanup] Error during cleanup run");
                }

                await Task.Delay(RunInterval, stoppingToken);
            }
        }

        private async Task RunCleanupAsync(CancellationToken stoppingToken)
        {
            List<string> userIds = await userMoviesRepository.GetUserIdsWithLargeWatchHistory(WatchHistoryThreshold);
            logger.LogInformation("[WatchHistoryCleanup] Found {Count} users with >{Threshold} movies in watch history",
                userIds.Count, WatchHistoryThreshold);

            foreach (string userId in userIds)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    await CleanupUserWatchHistoryAsync(userId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[WatchHistoryCleanup] Failed to cleanup watch history for user {UserId}", userId);
                }
            }
        }

        public async Task CleanupUserWatchHistoryAsync(string userId)
        {
            UserMovies userMovies = await userMoviesRepository.GetUserMovies(userId);
            if (userMovies == null || userMovies.watchHistory == null || userMovies.watchHistory.Count <= WatchHistoryThreshold)
            {
                return;
            }

            List<string> favoriteNames = userMovies.favouriteMovies
                .Where(m => m.ReleaseDate.HasValue)
                .Select(m => $"{m.Title} ({m.ReleaseDate.Value.Year})")
                .ToList();

            List<string> watchHistoryNames = userMovies.watchHistory
                .Where(m => m.ReleaseDate.HasValue)
                .Select(m => $"{m.Title} ({m.ReleaseDate.Value.Year})")
                .ToList();

            if (favoriteNames.Count == 0 || watchHistoryNames.Count == 0)
            {
                return;
            }

            string irrelevantMovie = await groqClient.GetIrrelevantMovieFromHistoryAsync(favoriteNames, watchHistoryNames);
            if (string.IsNullOrWhiteSpace(irrelevantMovie))
            {
                logger.LogWarning("[WatchHistoryCleanup] Groq returned empty response for user {UserId}", userId);
                return;
            }

            // Re-read fresh to avoid overwriting concurrent user changes
            UserMovies freshUserMovies = await userMoviesRepository.GetUserMovies(userId);
            if (freshUserMovies == null || freshUserMovies.watchHistory == null)
            {
                return;
            }

            Movie movieToRemove = FindMovieInWatchHistory(freshUserMovies.watchHistory, irrelevantMovie.Trim());
            if (movieToRemove == null)
            {
                logger.LogWarning("[WatchHistoryCleanup] Could not match AI response '{Response}' to watch history for user {UserId}",
                    irrelevantMovie, userId);
                return;
            }

            // Add to history first — if the next step fails, a duplicate in history is safe
            await userMovieHistoryRepository.AddToHistory(userId, new List<Movie> { movieToRemove });

            // Re-read again to get the latest state before writing back
            freshUserMovies = await userMoviesRepository.GetUserMovies(userId);
            if (freshUserMovies?.watchHistory != null && freshUserMovies.watchHistory.Remove(movieToRemove))
            {
                await userMoviesRepository.UpsertUserMovies(freshUserMovies);
            }

            logger.LogInformation("[WatchHistoryCleanup] Moved '{Title}' from watch history to history table for user {UserId}",
                movieToRemove.Title, userId);
        }

        private static Movie FindMovieInWatchHistory(HashSet<Movie> watchHistory, string aiResponse)
        {
            // Try exact match first: "Title (Year)"
            foreach (Movie movie in watchHistory)
            {
                if (!movie.ReleaseDate.HasValue) continue;
                string formatted = $"{movie.Title} ({movie.ReleaseDate.Value.Year})";
                if (string.Equals(formatted, aiResponse, StringComparison.OrdinalIgnoreCase))
                {
                    return movie;
                }
            }

            // Fallback: try matching title only (AI might return slightly different format)
            string responseLower = aiResponse.ToLowerInvariant();
            foreach (Movie movie in watchHistory)
            {
                if (movie.Title != null && responseLower.Contains(movie.Title.ToLowerInvariant()))
                {
                    return movie;
                }
            }

            return null;
        }
    }
}
