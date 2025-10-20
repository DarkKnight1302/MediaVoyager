using System.Threading.Tasks;
using MediaVoyager.Entities;
using Microsoft.Azure.Cosmos;
using NewHorizonLib.Services;

namespace MediaVoyager.Repositories
{
    public class MovieCacheRepository : ICacheRepository<MovieCache>
    {
        private readonly ICosmosDbService cosmosDbService;

        public MovieCacheRepository(ICosmosDbService cosmosDbService)
        {
            this.cosmosDbService = cosmosDbService;
        }

        public async Task<MovieCache> GetAsync(string id)
        {
            var container = GetContainer();
            try
            {
                var resp = await container.ReadItemAsync<MovieCache>(id, new PartitionKey(id));
                return resp.Resource;
            }
            catch (CosmosException)
            {
                return null;
            }
        }

        public async Task<MovieCache> UpsertAsync(MovieCache item)
        {
            var container = GetContainer();
            var resp = await container.UpsertItemAsync(item, new PartitionKey(item.id));
            return resp.Resource;
        }

        private Container GetContainer()
        {
            return this.cosmosDbService.GetContainer("movieCache");
        }
    }
}
