using MediaVoyager.Entities;
using MediaVoyager.Models.Dashboard;
using MediaVoyager.Repositories;
using MediaVoyager.Services.Interfaces;
using Microsoft.Azure.Cosmos;
using NewHorizonLib.Services;

namespace MediaVoyager.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUserActivityRepository userActivityRepository;
        private readonly ICosmosDbService cosmosDbService;
        private readonly IApiRequestLogRepository apiRequestLogRepository;
        private readonly ILogger<DashboardService> logger;

        public DashboardService(
            IUserActivityRepository userActivityRepository,
            ICosmosDbService cosmosDbService,
            IApiRequestLogRepository apiRequestLogRepository,
            ILogger<DashboardService> logger)
        {
            this.userActivityRepository = userActivityRepository;
            this.cosmosDbService = cosmosDbService;
            this.apiRequestLogRepository = apiRequestLogRepository;
            this.logger = logger;
        }

        public async Task<DashboardMetrics> GetDashboardMetricsAsync(int days = 30)
        {
            var signupsTask = GetUserSignupMetricsAsync(days);
            var activeUsersTask = GetActiveUsersMetricsAsync(days);
            var recommendationsTask = GetRecommendationMetricsAsync(days);
            var searchesTask = GetSearchMetricsAsync(days);
            var watchlistTask = GetWatchlistMetricsAsync(days);
            var apiFailuresTask = GetApiFailureMetricsAsync(days);

            await Task.WhenAll(signupsTask, activeUsersTask, recommendationsTask, searchesTask, watchlistTask, apiFailuresTask);

            return new DashboardMetrics
            {
                UserSignups = await signupsTask,
                ActiveUsers = await activeUsersTask,
                Recommendations = await recommendationsTask,
                Searches = await searchesTask,
                Watchlist = await watchlistTask,
                ApiFailures = await apiFailuresTask
            };
        }

        public async Task<ApiFailureMetrics> GetApiFailureMetricsAsync(int days = 30)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var fromDate = now.AddDays(-days);

                var failures = await apiRequestLogRepository.GetFailureCountsByApiAndDateAsync(fromDate, now);
                return new ApiFailureMetrics
                {
                    FailuresByApiAndDate = failures
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting API failure metrics");
                return new ApiFailureMetrics();
            }
        }

        public async Task<UserSignupMetrics> GetUserSignupMetricsAsync(int days = 30)
        {
            try
            {
                var container = cosmosDbService.GetContainer("User");
                var fromDate = DateTimeOffset.UtcNow.AddDays(-days);

                // Get all users
                var query = new QueryDefinition("SELECT c.createdAt FROM c");
                var users = new List<DateTimeOffset>();

                using var iterator = container.GetItemQueryIterator<dynamic>(query);
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (var item in response)
                    {
                        if (item.createdAt != null)
                        {
                            users.Add((DateTimeOffset)item.createdAt);
                        }
                    }
                }

                var signupsByDate = users
                   .Where(d => d >= fromDate)
                 .GroupBy(d => d.Date)
                    .Select(g => new DateCount { Date = g.Key, Count = g.Count() })
              .OrderBy(x => x.Date)
                   .ToList();

                return new UserSignupMetrics
                {
                    TotalUsers = users.Count,
                    SignupsByDate = signupsByDate
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting user signup metrics");
                return new UserSignupMetrics();
            }
        }

        public async Task<ActiveUsersMetrics> GetActiveUsersMetricsAsync(int days = 30)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var dailyFromDate = now.AddDays(-1);
                var monthlyFromDate = now.AddDays(-30);
                var fromDate = now.AddDays(-days);

                var dailyActiveUsers = await userActivityRepository.GetUniqueActiveUsersCountAsync(dailyFromDate, now);
                var monthlyActiveUsers = await userActivityRepository.GetUniqueActiveUsersCountAsync(monthlyFromDate, now);
                var dailyActiveUsersByDate = await userActivityRepository.GetDailyActiveUsersAsync(fromDate, now);

                return new ActiveUsersMetrics
                {
                    DailyActiveUsers = dailyActiveUsers,
                    MonthlyActiveUsers = monthlyActiveUsers,
                    DailyActiveUsersByDate = dailyActiveUsersByDate
                    .Select(kvp => new DateCount { Date = kvp.Key, Count = kvp.Value })
                    .OrderBy(x => x.Date)
                    .ToList()
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting active users metrics");
                return new ActiveUsersMetrics();
            }
        }

        public async Task<RecommendationMetrics> GetRecommendationMetricsAsync(int days = 30)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var fromDate = now.AddDays(-days);

                var movieRecommendations = await userActivityRepository.GetActivityCountByDateAsync(
                ActivityTypes.MovieRecommendation, fromDate, now);
                var tvRecommendations = await userActivityRepository.GetActivityCountByDateAsync(
                             ActivityTypes.TvRecommendation, fromDate, now);

                return new RecommendationMetrics
                {
                    TotalMovieRecommendations = movieRecommendations.Values.Sum(),
                    TotalTvRecommendations = tvRecommendations.Values.Sum(),
                    MovieRecommendationsByDate = movieRecommendations
                     .Select(kvp => new DateCount { Date = kvp.Key, Count = kvp.Value })
                .OrderBy(x => x.Date)
                 .ToList(),
                    TvRecommendationsByDate = tvRecommendations
                 .Select(kvp => new DateCount { Date = kvp.Key, Count = kvp.Value })
                   .OrderBy(x => x.Date)
              .ToList()
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting recommendation metrics");
                return new RecommendationMetrics();
            }
        }

        public async Task<SearchMetrics> GetSearchMetricsAsync(int days = 30)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var fromDate = now.AddDays(-days);

                var movieSearches = await userActivityRepository.GetActivityCountByDateAsync(
                   ActivityTypes.MovieSearch, fromDate, now);
                var tvSearches = await userActivityRepository.GetActivityCountByDateAsync(
            ActivityTypes.TvSearch, fromDate, now);

                // Get unique users who searched
                var movieSearchActivities = await userActivityRepository.GetActivitiesByTypeAsync(
                    ActivityTypes.MovieSearch, fromDate, now);
                var tvSearchActivities = await userActivityRepository.GetActivitiesByTypeAsync(
                 ActivityTypes.TvSearch, fromDate, now);

                var allSearchUserIds = movieSearchActivities.Select(a => a.UserId)
                        .Union(tvSearchActivities.Select(a => a.UserId))
                    .Distinct();

                // Unique users by date
                var allSearchActivities = movieSearchActivities.Concat(tvSearchActivities);
                var uniqueUsersByDate = allSearchActivities
                  .GroupBy(a => a.ActivityDate.Date)
               .Select(g => new DateCount
               {
                   Date = g.Key,
                   Count = g.Select(a => a.UserId).Distinct().Count()
               })
                   .OrderBy(x => x.Date)
                      .ToList();

                return new SearchMetrics
                {
                    TotalMovieSearches = movieSearches.Values.Sum(),
                    TotalTvSearches = tvSearches.Values.Sum(),
                    UniqueUsersSearched = allSearchUserIds.Count(),
                    MovieSearchesByDate = movieSearches
                    .Select(kvp => new DateCount { Date = kvp.Key, Count = kvp.Value })
                    .OrderBy(x => x.Date)
                    .ToList(),
                    TvSearchesByDate = tvSearches
                    .Select(kvp => new DateCount { Date = kvp.Key, Count = kvp.Value })
                    .OrderBy(x => x.Date)
                    .ToList(),
                    UniqueUsersByDate = uniqueUsersByDate
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting search metrics");
                return new SearchMetrics();
            }
        }

        public async Task<WatchlistMetrics> GetWatchlistMetricsAsync(int days = 30)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var fromDate = now.AddDays(-days);

                var movieWatchlist = await userActivityRepository.GetActivityCountByDateAsync(
                    ActivityTypes.AddMovieToWatchlist, fromDate, now);
                var tvWatchlist = await userActivityRepository.GetActivityCountByDateAsync(
                  ActivityTypes.AddTvToWatchlist, fromDate, now);

                // Get unique users with watchlist activity
                var movieWatchlistUsers = await userActivityRepository.GetUniqueUsersWithActivityAsync(
                 ActivityTypes.AddMovieToWatchlist, fromDate, now);
                var tvWatchlistUsers = await userActivityRepository.GetUniqueUsersWithActivityAsync(
           ActivityTypes.AddTvToWatchlist, fromDate, now);

                // Combine all watchlist activities by date
                var allWatchlistByDate = new Dictionary<DateTime, int>();
                foreach (var kvp in movieWatchlist)
                {
                    allWatchlistByDate[kvp.Key] = kvp.Value;
                }
                foreach (var kvp in tvWatchlist)
                {
                    if (allWatchlistByDate.ContainsKey(kvp.Key))
                        allWatchlistByDate[kvp.Key] += kvp.Value;
                    else
                        allWatchlistByDate[kvp.Key] = kvp.Value;
                }

                // Get all unique users (combine movie and tv)
                var movieWatchlistActivities = await userActivityRepository.GetActivitiesByTypeAsync(
                      ActivityTypes.AddMovieToWatchlist, fromDate, now);
                var tvWatchlistActivities = await userActivityRepository.GetActivitiesByTypeAsync(
           ActivityTypes.AddTvToWatchlist, fromDate, now);
                var uniqueUsers = movieWatchlistActivities.Select(a => a.UserId)
              .Union(tvWatchlistActivities.Select(a => a.UserId))
                 .Distinct()
              .Count();

                return new WatchlistMetrics
                {
                    TotalMoviesAddedToWatchlist = movieWatchlist.Values.Sum(),
                    TotalTvAddedToWatchlist = tvWatchlist.Values.Sum(),
                    UniqueUsersWithWatchlist = uniqueUsers,
                    WatchlistActivityByDate = allWatchlistByDate
                     .Select(kvp => new DateCount { Date = kvp.Key, Count = kvp.Value })
               .OrderBy(x => x.Date)
                .ToList()
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting watchlist metrics");
                return new WatchlistMetrics();
            }
        }
    }
}
