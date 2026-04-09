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

        public async Task<UserMovies> CreateUserMovies(string userId, List<Movie> favourites, List<Movie> watchHistory)
        {
            var container = GetContainer();
            UserMovies userMovies = new UserMovies()
            {
                id = userId,
                favouriteMovies = favourites.ToHashSet(),
                watchHistory = watchHistory.ToHashSet()
            };

            var response = await container.CreateItemAsync<UserMovies>(userMovies, new PartitionKey(userId));
            return response.Resource;
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

        public async Task UpsertUserMovies(UserMovies userMovies)
        {
            var container = GetContainer();
            await container.UpsertItemAsync<UserMovies>(userMovies, new PartitionKey(userMovies.id));
        }

        public async Task RemoveFavourites(string userId, List<string> movieIds)
        {
            UserMovies userMovies = await this.GetUserMovies(userId);
            if (userMovies == null)
            {
                return;
            }

            var moviesToRemove = userMovies.favouriteMovies.Where(m => movieIds.Contains(m.Id)).ToList();
            foreach (var movie in moviesToRemove)
            {
                userMovies.favouriteMovies.Remove(movie);
            }

            var container = this.GetContainer();
            await container.UpsertItemAsync<UserMovies>(userMovies, new PartitionKey(userId));
        }

        public async Task<List<string>> GetUserIdsWithLargeWatchHistory(int threshold)
        {
            var container = GetContainer();
            var query = new QueryDefinition("SELECT c.id FROM c WHERE ARRAY_LENGTH(c.watchHistory) > @threshold")
                .WithParameter("@threshold", threshold);
            var userIds = new List<string>();
            using var iterator = container.GetItemQueryIterator<UserIdProjection>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                userIds.AddRange(response.Select(u => u.id));
            }
            return userIds;
        }

        private class UserIdProjection
        {
            public string id { get; set; }
        }

        private Container GetContainer()
        {
            return this.cosmosDbService.GetContainer("UserMovies");
        }
    }
}
