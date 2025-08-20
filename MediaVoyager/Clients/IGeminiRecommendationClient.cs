namespace MediaVoyager.Clients
{
    public interface IGeminiRecommendationClient
    {
        public Task<string> GetMovieRecommendationAsync(List<string> favoriteMovies, List<string> watchHistory);
    }
}
