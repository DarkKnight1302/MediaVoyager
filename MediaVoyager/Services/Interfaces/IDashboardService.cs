using MediaVoyager.Models.Dashboard;

namespace MediaVoyager.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardMetrics> GetDashboardMetricsAsync(int days = 30);
  Task<UserSignupMetrics> GetUserSignupMetricsAsync(int days = 30);
        Task<ActiveUsersMetrics> GetActiveUsersMetricsAsync(int days = 30);
        Task<RecommendationMetrics> GetRecommendationMetricsAsync(int days = 30);
        Task<SearchMetrics> GetSearchMetricsAsync(int days = 30);
        Task<WatchlistMetrics> GetWatchlistMetricsAsync(int days = 30);
        Task<ApiFailureMetrics> GetApiFailureMetricsAsync(int days = 30);
        Task<LoginMetrics> GetLoginMetricsAsync(int days = 30);
        Task<FavouritesMetrics> GetFavouritesMetricsAsync(int days = 30);
        Task<ContentEngagementMetrics> GetContentEngagementMetricsAsync(int days = 30);
        Task<UserRetentionMetrics> GetUserRetentionMetricsAsync(int days = 30);
    }
}
