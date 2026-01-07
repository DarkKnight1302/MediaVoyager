namespace MediaVoyager.Clients
{
    public interface IRecommendationClient
    {
        Task<string> GetMovieRecommendationAsync(List<string> favoriteMovies, List<string> watchHistory, double temperature);
        Task<string> GetTvShowRecommendationAsync(List<string> favouriteTvShows, List<string> watchHistory, double temperature);
    }
}

