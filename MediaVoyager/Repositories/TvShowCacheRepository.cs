using System.Threading.Tasks;
using MediaVoyager.Entities;
using Microsoft.Azure.Cosmos;
using NewHorizonLib.Services;

namespace MediaVoyager.Repositories
{
    public class TvShowCacheRepository : ICacheRepository<TvShowCache>
    {
        private readonly ICosmosDbService cosmosDbService;

        public TvShowCacheRepository(ICosmosDbService cosmosDbService)
        {
            this.cosmosDbService = cosmosDbService;
        }

        public async Task<TvShowCache> GetAsync(string id)
        {
            var container = GetContainer();
            try
            {
                var resp = await container.ReadItemAsync<TvShowCache>(id, new PartitionKey(id));
                return resp.Resource;
            }
            catch (CosmosException)
            {
                return null;
            }
        }

        public async Task<TvShowCache> UpsertAsync(TvShowCache item)
        {
            var container = GetContainer();
            var resp = await container.UpsertItemAsync(item, new PartitionKey(item.id));
            return resp.Resource;
        }

        private Container GetContainer()
        {
            return this.cosmosDbService.GetContainer("tvShowCache");
        }
    }
}
