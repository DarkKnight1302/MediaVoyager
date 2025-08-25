using MediaVoyager.Clients;
using MediaVoyager.Entities;
using MediaVoyager.Models;
using MediaVoyager.Repositories;
using MediaVoyager.Services.Interfaces;
using NewHorizonLib.Services;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using Movie = MediaVoyager.Models.Movie;

namespace MediaVoyager.Services
{
    public class MediaRecommendationService : IMediaRecommendationService
    {
        private readonly TMDbClient tmdbClient;
        private readonly IUserMoviesRepository userMoviesRepository;
        private readonly IGeminiRecommendationClient geminiRecommendationClient;

        public MediaRecommendationService(ISecretService secretService,
            IUserMoviesRepository userMoviesRepository,
            IGeminiRecommendationClient geminiRecommendationClient)
        {
            string tmdbApiKey = secretService.GetSecretValue("tmdb_api_key");
            this.tmdbClient = new TMDbClient(tmdbApiKey);
            this.userMoviesRepository = userMoviesRepository;
            this.geminiRecommendationClient = geminiRecommendationClient;
        }

        public async Task<MovieResponse> GetMovieRecommendationForUser(string userId)
        {
            UserMovies userMovies = await this.userMoviesRepository.GetUserMovies(userId).ConfigureAwait(false);
            if (userMovies == null)
            {
                return null;
            }
            List<string> favouriteMovies = userMovies.favouriteMovies.Select(x => ConvertToEasyName(x)).ToList<string>();
            List<string> watchHistory = userMovies.watchHistory.Select(x => ConvertToEasyName(x)).ToList<string>();
            string movie = await this.geminiRecommendationClient.GetMovieRecommendationAsync(favouriteMovies, watchHistory, 1);
            if (string.IsNullOrEmpty(movie))
            {
                movie = await this.geminiRecommendationClient.GetMovieRecommendationAsync(favouriteMovies, watchHistory, 2);
            }
            if (string.IsNullOrEmpty(movie))
            {
                return null;
            }
            SearchContainer<SearchMovie> results = await this.tmdbClient.SearchMovieAsync(movie);
            if (results.Results.Count > 0)
            {
                int movieId = results.Results[0].Id;
                TMDbLib.Objects.Movies.Movie movieTmdb = await this.tmdbClient.GetMovieAsync(movieId);
                if (movieTmdb != null)
                {
                    MovieResponse movieResponse = new MovieResponse()
                    {
                        Id = movieId.ToString(),
                         Genres = movieTmdb.Genres,
                          Logo = movieTmdb.
                    };
                }
            }
        }

        private string ConvertToEasyName(Movie movie)
        {
            return $"{movie.Title} ({movie.ReleaseDate})";
        }
    }
}
