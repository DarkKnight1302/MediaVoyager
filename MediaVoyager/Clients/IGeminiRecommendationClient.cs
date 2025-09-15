namespace MediaVoyager.Clients
{
    public interface IGeminiRecommendationClient
    {
        public Task<string> GetMovieRecommendationAsync(List<string> favoriteMovies, List<string> watchHistory, int temperature);

        public Task<string> GetTvShowRecommendationAsync(List<string> favouriteTvShows, List<string> watchHistory, int temperature);
    }
}
