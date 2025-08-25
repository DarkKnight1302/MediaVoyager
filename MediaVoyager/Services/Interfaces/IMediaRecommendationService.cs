using MediaVoyager.Models;

namespace MediaVoyager.Services.Interfaces
{
    public interface IMediaRecommendationService
    {
        public Task<MovieResponse> GetMovieRecommendationForUser(string userId);
    }
}
