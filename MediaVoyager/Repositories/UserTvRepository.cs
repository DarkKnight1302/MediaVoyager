using MediaVoyager.Entities;
using MediaVoyager.Models;
using Microsoft.Azure.Cosmos;
using NewHorizonLib.Services;

namespace MediaVoyager.Repositories
{
    public class UserTvRepository : IUserTvRepository
    {
        private readonly ICosmosDbService cosmosDbService;

        public UserTvRepository(ICosmosDbService cosmosDbService)
        {
            this.cosmosDbService = cosmosDbService;
        }

        public async Task AddFavourites(string userId, List<TvShow> favourites)
        {
            UserTv userTv = await this.GetUserTv(userId).ConfigureAwait(false);
            userTv.favouriteTv.AddRange(favourites.Where(f => !userTv.favouriteTv.Contains(f)));
            userTv.watchHistory.AddRange(favourites.Where(f => !userTv.watchHistory.Contains(f)));
            var container = this.GetContainer();
            await container.UpsertItemAsync<UserTv>(userTv, new PartitionKey(userId));
        }

        public async Task AddWatchHistory(string userId, List<TvShow> watchHistory)
        {
            UserTv userTv = await this.GetUserTv(userId);
            userTv.watchHistory.AddRange(watchHistory.Where(w => !userTv.watchHistory.Contains(w)));
            var container = this.GetContainer();
            await container.UpsertItemAsync<UserTv>(userTv, new PartitionKey(userId));
        }

        public async Task<UserTv> CreateUserTv(string userId, List<TvShow> favourites, List<TvShow> watchHistory)
        {
            var container = GetContainer();
            UserTv userTv = new UserTv()
            {
                id = userId,
                favouriteTv = favourites,
                watchHistory = watchHistory
            };

            var response = await container.CreateItemAsync<UserTv>(userTv, new PartitionKey(userId));
            return response.Resource;
        }

        public async Task<UserTv> GetUserTv(string userId)
        {
            var container = GetContainer();
            try
            {
                var itemResponse = await container.ReadItemAsync<UserTv>(userId, new PartitionKey(userId));
                return itemResponse.Resource;
            }
            catch (CosmosException)
            {
                return null;
            }
        }

        public async Task UpsertUserTv(UserTv userTv)
        {
            var container = GetContainer();
            await container.UpsertItemAsync<UserTv>(userTv, new PartitionKey(userTv.id));
        }

        private Container GetContainer()
        {
            return this.cosmosDbService.GetContainer("UserTv");
        }
    }
}