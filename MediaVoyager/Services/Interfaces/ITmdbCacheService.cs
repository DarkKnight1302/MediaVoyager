using System.Threading.Tasks;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.TvShows;

namespace MediaVoyager.Services.Interfaces
{
    public interface ITmdbCacheService
    {
        Task<Movie> GetMovieAsync(int id);
        Task<TvShow> GetTvShowAsync(int id);
    }
}
