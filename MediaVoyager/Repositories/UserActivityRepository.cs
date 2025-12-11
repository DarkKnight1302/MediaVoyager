using MediaVoyager.Entities;
using Microsoft.Azure.Cosmos;
using NewHorizonLib.Services;

namespace MediaVoyager.Repositories
{
    public class UserActivityRepository : IUserActivityRepository
    {
        private readonly ICosmosDbService cosmosDbService;
        private readonly ILogger<UserActivityRepository> logger;
        private bool _containerChecked = false;

        public UserActivityRepository(ICosmosDbService cosmosDbService, ILogger<UserActivityRepository> logger)
        {
            this.cosmosDbService = cosmosDbService;
            this.logger = logger;
        }

        public async Task LogActivityAsync(string userId, string activityType, string details = null)
        {
            try
            {
                var container = GetContainer();
                var activity = new UserActivity
                {
                    id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    ActivityType = activityType,
                    ActivityDate = DateTimeOffset.UtcNow,
                    Details = details,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await container.CreateItemAsync(activity, new PartitionKey(activity.id));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Container doesn't exist - log warning but don't fail
                if (!_containerChecked)
                {
                    logger.LogWarning("UserActivity container does not exist. Activity tracking is disabled. Please create the 'UserActivity' container in CosmosDB.");
                    _containerChecked = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to log activity for user {UserId}, type {ActivityType}", userId, activityType);
            }
        }

        public async Task<List<UserActivity>> GetActivitiesAsync(DateTimeOffset fromDate, DateTimeOffset toDate)
        {
            var container = GetContainer();
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.ActivityDate >= @fromDate AND c.ActivityDate <= @toDate")
                .WithParameter("@fromDate", fromDate)
                .WithParameter("@toDate", toDate);

            return await ExecuteQueryAsync(container, query);
        }

        public async Task<List<UserActivity>> GetActivitiesByTypeAsync(string activityType, DateTimeOffset fromDate, DateTimeOffset toDate)
        {
            var container = GetContainer();
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.ActivityType = @activityType AND c.ActivityDate >= @fromDate AND c.ActivityDate <= @toDate")
                .WithParameter("@activityType", activityType)
                .WithParameter("@fromDate", fromDate)
                .WithParameter("@toDate", toDate);

            return await ExecuteQueryAsync(container, query);
        }

        public async Task<List<UserActivity>> GetUserActivitiesAsync(string userId, DateTimeOffset fromDate, DateTimeOffset toDate)
        {
            var container = GetContainer();
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.UserId = @userId AND c.ActivityDate >= @fromDate AND c.ActivityDate <= @toDate")
                .WithParameter("@userId", userId)
                .WithParameter("@fromDate", fromDate)
                .WithParameter("@toDate", toDate);

            return await ExecuteQueryAsync(container, query);
        }

        public async Task<int> GetUniqueActiveUsersCountAsync(DateTimeOffset fromDate, DateTimeOffset toDate)
        {
            var container = GetContainer();
            var query = new QueryDefinition(
                "SELECT VALUE COUNT(1) FROM (SELECT DISTINCT c.UserId FROM c WHERE c.ActivityDate >= @fromDate AND c.ActivityDate <= @toDate)")
                .WithParameter("@fromDate", fromDate)
                .WithParameter("@toDate", toDate);

            using var iterator = container.GetItemQueryIterator<int>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }
            return 0;
        }

        public async Task<Dictionary<DateTime, int>> GetDailyActiveUsersAsync(DateTimeOffset fromDate, DateTimeOffset toDate)
        {
            var activities = await GetActivitiesAsync(fromDate, toDate);

            return activities
                .GroupBy(a => a.ActivityDate.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(a => a.UserId).Distinct().Count()
                );
        }

        public async Task<Dictionary<DateTime, int>> GetActivityCountByDateAsync(string activityType, DateTimeOffset fromDate, DateTimeOffset toDate)
        {
            var activities = await GetActivitiesByTypeAsync(activityType, fromDate, toDate);

            return activities
                .GroupBy(a => a.ActivityDate.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()
                );
        }

        public async Task<int> GetUniqueUsersWithActivityAsync(string activityType, DateTimeOffset fromDate, DateTimeOffset toDate)
        {
            var activities = await GetActivitiesByTypeAsync(activityType, fromDate, toDate);
            return activities.Select(a => a.UserId).Distinct().Count();
        }

        private async Task<List<UserActivity>> ExecuteQueryAsync(Container container, QueryDefinition query)
        {
            var results = new List<UserActivity>();
            using var iterator = container.GetItemQueryIterator<UserActivity>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        private Container GetContainer()
        {
            return this.cosmosDbService.GetContainer("UserActivity");
        }
    }
}
