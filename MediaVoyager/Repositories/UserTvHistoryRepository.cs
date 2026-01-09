using MediaVoyager.Entities;
using MediaVoyager.Models;
using Microsoft.Azure.Cosmos;
using NewHorizonLib.Services;

namespace MediaVoyager.Repositories
{
    public class UserTvHistoryRepository : IUserTvHistoryRepository
    {
        private readonly ICosmosDbService cosmosDbService;

        public UserTvHistoryRepository(ICosmosDbService cosmosDbService)
        {
            this.cosmosDbService = cosmosDbService;
        }

        public async Task<UserTvHistory> GetUserTvHistory(string userId)
        {
            var container = GetContainer();
            try
            {
                var itemResponse = await container.ReadItemAsync<UserTvHistory>(userId, new PartitionKey(userId));
                return itemResponse.Resource;
            }
            catch (CosmosException)
            {
                return null;
            }
        }

        public async Task<UserTvHistory> CreateUserTvHistory(string userId, List<TvShow> tvShows)
        {
            var container = GetContainer();
            var history = new UserTvHistory
            {
                id = userId,
                tvShows = tvShows.ToHashSet()
            };

            var response = await container.CreateItemAsync(history, new PartitionKey(userId));
            return response.Resource;
        }

        public async Task AddToHistory(string userId, List<TvShow> tvShows)
        {
            var history = await GetUserTvHistory(userId);
            if (history == null)
            {
                await CreateUserTvHistory(userId, tvShows);
                return;
            }

            history.tvShows.UnionWith(tvShows);
            await UpsertUserTvHistory(history);
        }

        public async Task UpsertUserTvHistory(UserTvHistory history)
        {
            var container = GetContainer();
            await container.UpsertItemAsync(history, new PartitionKey(history.id));
        }

        public async Task RemoveFromHistory(string userId, List<string> tvIds)
        {
            var history = await GetUserTvHistory(userId);
            if (history == null)
            {
                return;
            }

            var tvShowsToRemove = history.tvShows.Where(tv => tvIds.Contains(tv.Id)).ToList();
            foreach (var tvShow in tvShowsToRemove)
            {
                history.tvShows.Remove(tvShow);
            }

            await UpsertUserTvHistory(history);
        }

        private Container GetContainer()
        {
            return cosmosDbService.GetContainer("UserTvHistory");
        }
    }
}
