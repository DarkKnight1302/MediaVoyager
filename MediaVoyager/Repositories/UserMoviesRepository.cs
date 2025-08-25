using GoogleApi.Entities.Search.Video.Common;
using MediaVoyager.Entities;
using MediaVoyager.Models;
using Microsoft.Azure.Cosmos;
using NewHorizonLib.Services;

namespace MediaVoyager.Repositories
{
    public class UserMoviesRepository : IUserMoviesRepository
    {
        private readonly ICosmosDbService cosmosDbService;

        public UserMoviesRepository(ICosmosDbService cosmosDbService)
        {
            this.cosmosDbService = cosmosDbService;
        }

        public async Task AddFavourites(string userId, List<Movie> favourites)
        {
            UserMovies userMovies = await this.GetUserMovies(userId).ConfigureAwait(false);
            userMovies.favouriteMovies.UnionWith(favourites);
            userMovies.watchHistory.UnionWith(favourites);
            var container = this.GetContainer();
            await container.UpsertItemAsync<UserMovies>(userMovies, new PartitionKey(userId));
        }

        public async Task AddWatchHistory(string userId, List<Movie> watchHistory)
        {
            UserMovies userMovies = await this.GetUserMovies(userId);
            userMovies.watchHistory.UnionWith(watchHistory);
            var container = this.GetContainer();
            await container.UpsertItemAsync<UserMovies>(userMovies, new PartitionKey(userId));
        }

        public async Task CreateUserMovies(string userId, List<Movie> favourites, List<Movie> watchHistory)
        {
            var container = GetContainer();
            UserMovies userMovies = new UserMovies()
            {
                id = userId,
                favouriteMovies = favourites.ToHashSet(),
                watchHistory = watchHistory.ToHashSet()
            };

            await container.CreateItemAsync<UserMovies>(userMovies, new PartitionKey(userId));
        }

        public async Task<UserMovies> GetUserMovies(string userId)
        {
            var container = GetContainer();
            try
            {
                var itemResponse = await container.ReadItemAsync<UserMovies>(userId, new PartitionKey(userId));
                return itemResponse.Resource;
            }
            catch (CosmosException)
            {
                return null;
            }
        }

        private Container GetContainer()
        {
            return this.cosmosDbService.GetContainer("UserMovies");
        }
    }
}
