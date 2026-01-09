using MediaVoyager.Entities;
using MediaVoyager.Models;
using Microsoft.Azure.Cosmos;
using NewHorizonLib.Services;

namespace MediaVoyager.Repositories
{
    public class UserMovieHistoryRepository : IUserMovieHistoryRepository
    {
        private readonly ICosmosDbService cosmosDbService;

        public UserMovieHistoryRepository(ICosmosDbService cosmosDbService)
        {
            this.cosmosDbService = cosmosDbService;
        }

        public async Task<UserMovieHistory> GetUserMovieHistory(string userId)
        {
            var container = GetContainer();
            try
            {
                var itemResponse = await container.ReadItemAsync<UserMovieHistory>(userId, new PartitionKey(userId));
                return itemResponse.Resource;
            }
            catch (CosmosException)
            {
                return null;
            }
        }

        public async Task<UserMovieHistory> CreateUserMovieHistory(string userId, List<Movie> movies)
        {
            var container = GetContainer();
            var history = new UserMovieHistory
            {
                id = userId,
                movies = movies.ToHashSet()
            };

            var response = await container.CreateItemAsync(history, new PartitionKey(userId));
            return response.Resource;
        }

        public async Task AddToHistory(string userId, List<Movie> movies)
        {
            var history = await GetUserMovieHistory(userId);
            if (history == null)
            {
                await CreateUserMovieHistory(userId, movies);
                return;
            }

            history.movies.UnionWith(movies);
            await UpsertUserMovieHistory(history);
        }

        public async Task UpsertUserMovieHistory(UserMovieHistory history)
        {
            var container = GetContainer();
            await container.UpsertItemAsync(history, new PartitionKey(history.id));
        }

        public async Task RemoveFromHistory(string userId, List<string> movieIds)
        {
            var history = await GetUserMovieHistory(userId);
            if (history == null)
            {
                return;
            }

            var moviesToRemove = history.movies.Where(m => movieIds.Contains(m.Id)).ToList();
            foreach (var movie in moviesToRemove)
            {
                history.movies.Remove(movie);
            }

            await UpsertUserMovieHistory(history);
        }

        private Container GetContainer()
        {
            return cosmosDbService.GetContainer("UserMovieHistory");
        }
    }
}
