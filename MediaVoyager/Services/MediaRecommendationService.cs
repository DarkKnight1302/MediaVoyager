using MediaVoyager.Clients;
using MediaVoyager.Entities;
using MediaVoyager.Models;
using MediaVoyager.Repositories;
using MediaVoyager.Services.Interfaces;
using NewHorizonLib.Services;
using TMDbLib.Client;
using TMDbLib.Objects.Find;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using Movie = MediaVoyager.Models.Movie;

namespace MediaVoyager.Services
{
    public class MediaRecommendationService : IMediaRecommendationService
    {
        private readonly IUserMoviesRepository userMoviesRepository;
        private readonly IGeminiRecommendationClient geminiRecommendationClient;
        private readonly string tmdbApiKey;

        public MediaRecommendationService(ISecretService secretService,
            IUserMoviesRepository userMoviesRepository,
            IGeminiRecommendationClient geminiRecommendationClient)
        {
            this.tmdbApiKey = secretService.GetSecretValue("tmdb_api_key");
            this.userMoviesRepository = userMoviesRepository;
            this.geminiRecommendationClient = geminiRecommendationClient;
        }

        public async Task<MovieResponse> GetMovieRecommendationForUser(string userId)
        {
            TMDbClient tmdbClient = new TMDbClient(tmdbApiKey);
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
            FindContainer findContainer = await tmdbClient.FindAsync(FindExternalSource.Imdb, movie).ConfigureAwait(false);

            List<SearchMovie> results = findContainer.MovieResults;

            if (results!= null && results.Count > 0)
            {
                int movieId = results[0].Id;
                TMDbLib.Objects.Movies.Movie movieTmdb = await tmdbClient.GetMovieAsync(movieId);
                if (movieTmdb != null)
                {
                    MovieResponse movieResponse = new MovieResponse()
                    {
                        Id = movieId.ToString(),
                        Genres = movieTmdb.Genres,
                        Poster = movieTmdb.PosterPath,
                        OriginCountry = movieTmdb.ProductionCountries[0].Name,
                        OverView = movieTmdb.Overview,
                        ReleaseDate = movieTmdb.ReleaseDate,
                        TagLine = movieTmdb.Tagline,
                        Title = movieTmdb.Title
                    };
                    return movieResponse;
                }
            }
            return null;
        }

        public Task<object> GetTvShowRecommendationForUser(string userId)
        {
            throw new NotImplementedException();
        }

        private string ConvertToEasyName(Movie movie)
        {
            return $"{movie.Title} ({movie.ReleaseDate.Value.Year})";
        }
    }
}
