using Microsoft.Azure.Cosmos;
using NewHorizonLib.Services;
using User = MediaVoyager.Entities.User;

namespace MediaVoyager.Repositories
{
    public class UserRepository : IUserRepository
    {
        
        private readonly ICosmosDbService cosmosDbService;
        private readonly ILogger<UserRepository> logger;

        public UserRepository(ICosmosDbService cosmosDbService, ILogger<UserRepository> logger)
        {
            this.cosmosDbService = cosmosDbService;
            this.logger = logger;
        }

        public async Task<Entities.User> CreateUser(Entities.User user)
        {
            var container = GetContainer();
            user.updatedAt = DateTimeOffset.UtcNow;
            user.createdAt = DateTimeOffset.UtcNow;
            user.movieWatchlist ??= new HashSet<string>();
            user.tvWatchlist ??= new HashSet<string>();
            
            ItemResponse<User> resp = await container.CreateItemAsync<User>(user, new PartitionKey(user.id));
            return resp.Resource;
        }

        public async Task<Entities.User> CreateUser(string id, string name, bool isGoogleLogin, string email, string passwordHash)
        {
            var container = GetContainer();
            Entities.User user = new Entities.User()
            {
                id = id,
                name = name,
                email = email,
                passwordHash = passwordHash,
                googleLogin = isGoogleLogin,
                updatedAt = DateTimeOffset.UtcNow,
                createdAt = DateTimeOffset.UtcNow,
                movieWatchlist = new HashSet<string>(),
                tvWatchlist = new HashSet<string>()
            };
            ItemResponse<User> resp = await container.CreateItemAsync<User>(user, new PartitionKey(id));
            return resp.Resource;
        }

        public async Task<Entities.User> GetUser(string userId)
        {
            var container = GetContainer();
            try
            {
                var itemResponse = await container.ReadItemAsync<Entities.User>(userId, new PartitionKey(userId));
                var user = itemResponse.Resource;
                
                // Initialize watchlist properties if null (for backward compatibility)
                user.movieWatchlist ??= new HashSet<string>();
                user.tvWatchlist ??= new HashSet<string>();
                
                return user;
            }
            catch (CosmosException)
            {
                return null;
            }
        }

        public async Task<Entities.User> GetUserByEmail(string email)
        {
            var container = GetContainer();
            try
            {
                var query = "SELECT * FROM c WHERE c.email = @email";
                var queryDefinition = new QueryDefinition(query).WithParameter("@email", email);
                
                using var iterator = container.GetItemQueryIterator<Entities.User>(queryDefinition);
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    var user = response.FirstOrDefault();
                    if (user != null)
                    {
                        // Initialize watchlist properties if null (for backward compatibility)
                        user.movieWatchlist ??= new HashSet<string>();
                        user.tvWatchlist ??= new HashSet<string>();
                        return user;
                    }
                }
                return null;
            }
            catch (CosmosException)
            {
                return null;
            }
        }

        public async Task<Entities.User> UpdateUser(Entities.User user)
        {
            var container = GetContainer();
            user.updatedAt = DateTimeOffset.UtcNow;
            user.movieWatchlist ??= new HashSet<string>();
            user.tvWatchlist ??= new HashSet<string>();
            
            var response = await container.UpsertItemAsync(user, new PartitionKey(user.id));
            return response.Resource;
        }

        // Watchlist methods
        public async Task<Entities.User> AddMoviesToWatchlist(string userId, List<string> movieIds)
        {
            var user = await GetUser(userId);
            if (user == null)
            {
                throw new ArgumentException($"User with id {userId} not found");
            }

            user.movieWatchlist ??= new HashSet<string>();
            foreach (var movieId in movieIds)
            {
                user.movieWatchlist.Add(movieId);
            }

            return await UpdateUser(user);
        }

        public async Task<Entities.User> RemoveMoviesFromWatchlist(string userId, List<string> movieIds)
        {
            var user = await GetUser(userId);
            if (user == null)
            {
                throw new ArgumentException($"User with id {userId} not found");
            }

            user.movieWatchlist ??= new HashSet<string>();
            foreach (var movieId in movieIds)
            {
                user.movieWatchlist.Remove(movieId);
            }

            return await UpdateUser(user);
        }

        public async Task<Entities.User> AddTvShowsToWatchlist(string userId, List<string> tvIds)
        {
            var user = await GetUser(userId);
            if (user == null)
            {
                throw new ArgumentException($"User with id {userId} not found");
            }

            user.tvWatchlist ??= new HashSet<string>();
            foreach (var tvId in tvIds)
            {
                user.tvWatchlist.Add(tvId);
            }

            return await UpdateUser(user);
        }

        public async Task<Entities.User> RemoveTvShowsFromWatchlist(string userId, List<string> tvIds)
        {
            var user = await GetUser(userId);
            if (user == null)
            {
                throw new ArgumentException($"User with id {userId} not found");
            }

            user.tvWatchlist ??= new HashSet<string>();
            foreach (var tvId in tvIds)
            {
                user.tvWatchlist.Remove(tvId);
            }

            return await UpdateUser(user);
        }

        private Container GetContainer()
        {
            return this.cosmosDbService.GetContainer("User");
        }
    }
}
