using MediaVoyager.Models;

namespace MediaVoyager.Services.Interfaces
{
    public interface IMediaRecommendationService
    {
        public Task<MovieResponse> GetMovieRecommendationForUser(string userId, double temp = 0.9, int recurCount = 0);

        public Task<TvShowResponse> GetTvShowRecommendationForUser(string userId, double temp = 0.9, int recurCount = 0);
    }
}
