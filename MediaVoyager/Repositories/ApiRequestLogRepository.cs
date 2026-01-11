using MediaVoyager.Entities;
using MediaVoyager.Models.Dashboard;
using Microsoft.Azure.Cosmos;
using NewHorizonLib.Services;
using System.Net;

namespace MediaVoyager.Repositories
{
    public class ApiRequestLogRepository : IApiRequestLogRepository
    {
        private readonly ICosmosDbService cosmosDbService;
        private readonly ILogger<ApiRequestLogRepository> logger;
        private bool _containerChecked;

        public ApiRequestLogRepository(ICosmosDbService cosmosDbService, ILogger<ApiRequestLogRepository> logger)
        {
            this.cosmosDbService = cosmosDbService;
            this.logger = logger;
        }

        public async Task LogAsync(string api, string method, int statusCode, DateTimeOffset timestamp, string? userId = null, string? path = null)
        {
            try
            {
                var container = GetContainer();

                var log = new ApiRequestLog
                {
                    id = Guid.NewGuid().ToString(),
                    Api = api,
                    Method = method,
                    StatusCode = statusCode,
                    Timestamp = timestamp,
                    UserId = userId,
                    Path = path
                };

                await container.CreateItemAsync(log, new PartitionKey(log.id));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                if (!_containerChecked)
                {
                    logger.LogWarning("ApiRequestLog container does not exist. API request logging is disabled. Please create the 'ApiRequestLog' container in CosmosDB.");
                    _containerChecked = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to log API request for {Api} {Method} {StatusCode}", api, method, statusCode);
            }
        }

        public async Task<List<ApiDayFailureCount>> GetFailureCountsByApiAndDateAsync(DateTimeOffset fromDate, DateTimeOffset toDate)
        {
            var container = GetContainer();

            // Pull rows and aggregate in-memory to avoid relying on Cosmos GROUP BY/DateTime parsing behavior.
            var query = new QueryDefinition(
                "SELECT c.Api, c.Timestamp, c.StatusCode FROM c WHERE c.Timestamp >= @fromDate AND c.Timestamp <= @toDate AND c.StatusCode != 200")
                .WithParameter("@fromDate", fromDate)
                .WithParameter("@toDate", toDate);

            var rows = new List<(string Api, DateTimeOffset Timestamp)>();

            using var iterator = container.GetItemQueryIterator<dynamic>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    try
                    {
                        string api = item.Api;
                        DateTimeOffset ts = (DateTimeOffset)item.Timestamp;
                        rows.Add((api, ts));
                    }
                    catch
                    {
                        // ignore malformed rows
                    }
                }
            }

            return rows
                .GroupBy(r => new { r.Api, Date = r.Timestamp.Date })
                .Select(g => new ApiDayFailureCount
                {
                    Api = g.Key.Api,
                    Date = g.Key.Date,
                    Count = g.Count()
                })
                .OrderBy(x => x.Api)
                .ThenBy(x => x.Date)
                .ToList();
        }

        private Container GetContainer() => cosmosDbService.GetContainer("ApiRequestLog");
    }
}
