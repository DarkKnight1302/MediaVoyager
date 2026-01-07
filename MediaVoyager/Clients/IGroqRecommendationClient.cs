using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaVoyager.Clients
{
    public interface IGroqRecommendationClient
    {
        public Task<string> GetMovieRecommendationAsync(List<string> favoriteMovies, List<string> watchHistory, double temperature);

        public Task<string> GetTvShowRecommendationAsync(List<string> favouriteTvShows, List<string> watchHistory, double temperature);
    }
}
