using MediaVoyager.Models;

namespace MediaVoyager.Services.Interfaces
{
    public interface IMediaRecommendationService
    {
        public Task<MovieResponse> GetMovieRecommendationForUser(string userId);

        public Task<TvShowResponse> GetTvShowRecommendationForUser(string userId);
    }
}
