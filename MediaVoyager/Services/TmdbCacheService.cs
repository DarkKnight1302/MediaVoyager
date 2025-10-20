using System.Threading.Tasks;
using MediaVoyager.Entities;
using MediaVoyager.Repositories;
using MediaVoyager.Services.Interfaces;
using NewHorizonLib.Services;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.TvShows;

namespace MediaVoyager.Services
{
    // Wrapper over TMDbClient that checks Cosmos cache first for Movie/TvShow by id
    public class TmdbCacheService : ITmdbCacheService
    {
        private readonly TMDbClient tmdbClient;
        private readonly ICacheRepository<MovieCache> movieCacheRepository;
        private readonly ICacheRepository<TvShowCache> tvShowCacheRepository;

        public TmdbCacheService(ISecretService secretService,
            ICacheRepository<MovieCache> movieCacheRepository,
            ICacheRepository<TvShowCache> tvShowCacheRepository)
        {
            var apiKey = secretService.GetSecretValue("tmdb_api_key");
            this.tmdbClient = new TMDbClient(apiKey);
            this.movieCacheRepository = movieCacheRepository;
            this.tvShowCacheRepository = tvShowCacheRepository;
        }

        public async Task<Movie> GetMovieAsync(int id)
        {
            var cacheId = id.ToString();
            var cached = await movieCacheRepository.GetAsync(cacheId).ConfigureAwait(false);
            if (cached?.data != null)
            {
                return cached.data;
            }

            var movie = await tmdbClient.GetMovieAsync(id).ConfigureAwait(false);
            if (movie != null)
            {
                await movieCacheRepository.UpsertAsync(new MovieCache { id = cacheId, data = movie }).ConfigureAwait(false);
            }
            return movie;
        }

        public async Task<TvShow> GetTvShowAsync(int id)
        {
            var cacheId = id.ToString();
            var cached = await tvShowCacheRepository.GetAsync(cacheId).ConfigureAwait(false);
            if (cached?.data != null)
            {
                return cached.data;
            }

            var tv = await tmdbClient.GetTvShowAsync(id).ConfigureAwait(false);
            if (tv != null)
            {
                await tvShowCacheRepository.UpsertAsync(new TvShowCache { id = cacheId, data = tv }).ConfigureAwait(false);
            }
            return tv;
        }
    }
}
