using MediaVoyager.Entities;

namespace MediaVoyager.Repositories
{
    public interface IUserActivityRepository
    {
     Task LogActivityAsync(string userId, string activityType, string details = null);
      Task<List<UserActivity>> GetActivitiesAsync(DateTimeOffset fromDate, DateTimeOffset toDate);
        Task<List<UserActivity>> GetActivitiesByTypeAsync(string activityType, DateTimeOffset fromDate, DateTimeOffset toDate);
        Task<List<UserActivity>> GetUserActivitiesAsync(string userId, DateTimeOffset fromDate, DateTimeOffset toDate);
        Task<int> GetUniqueActiveUsersCountAsync(DateTimeOffset fromDate, DateTimeOffset toDate);
   Task<Dictionary<DateTime, int>> GetDailyActiveUsersAsync(DateTimeOffset fromDate, DateTimeOffset toDate);
        Task<Dictionary<DateTime, int>> GetActivityCountByDateAsync(string activityType, DateTimeOffset fromDate, DateTimeOffset toDate);
        Task<int> GetUniqueUsersWithActivityAsync(string activityType, DateTimeOffset fromDate, DateTimeOffset toDate);
    }
}
