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
                createdAt = DateTimeOffset.UtcNow
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
                return itemResponse.Resource;
            }
            catch (CosmosException)
            {
                return null;
            }
        }

        public async Task UpdateUser(Entities.User user)
        {
            var container = GetContainer();
            user.updatedAt = DateTimeOffset.UtcNow;
            await container.UpsertItemAsync(user, new PartitionKey(user.id));
        }

        private Container GetContainer()
        {
            return this.cosmosDbService.GetContainer("User");
        }
    }
}
