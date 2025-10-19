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
using TMDbLib.Objects.TvShows;
using Movie = MediaVoyager.Models.Movie;
using TvShow = MediaVoyager.Models.TvShow;

namespace MediaVoyager.Services
{
    public class MediaRecommendationService : IMediaRecommendationService
    {
        private readonly IUserMoviesRepository userMoviesRepository;
        private readonly IGeminiRecommendationClient geminiRecommendationClient;
        private readonly IUserTvRepository userTvRepository;
        private readonly string tmdbApiKey;

        public MediaRecommendationService(ISecretService secretService,
            IUserMoviesRepository userMoviesRepository,
            IGeminiRecommendationClient geminiRecommendationClient,
            IUserTvRepository userTvRepository)
        {
            this.tmdbApiKey = secretService.GetSecretValue("tmdb_api_key");
            this.userMoviesRepository = userMoviesRepository;
            this.geminiRecommendationClient = geminiRecommendationClient;
            this.userTvRepository = userTvRepository;
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

        public async Task<TvShowResponse> GetTvShowRecommendationForUser(string userId)
        {
            TMDbClient tmdbClient = new TMDbClient(tmdbApiKey);
            UserTv userTvShows = await this.userTvRepository.GetUserTv(userId).ConfigureAwait(false);
            if (userTvShows == null)
            {
                return null;
            }
            List<string> favouriteTvShows = userTvShows.favouriteTv.Select(x => ConvertToEasyName(x)).ToList<string>();
            List<string> watchHistory = userTvShows.watchHistory.Select(x => ConvertToEasyName(x)).ToList<string>();
            string tvShow = await this.geminiRecommendationClient.GetTvShowRecommendationAsync(favouriteTvShows, watchHistory, 1);
            if (string.IsNullOrEmpty(tvShow))
            {
                tvShow = await this.geminiRecommendationClient.GetTvShowRecommendationAsync(favouriteTvShows, watchHistory, 2);
            }
            if (string.IsNullOrEmpty(tvShow))
            {
                return null;
            }
            string[] tvShowParts = tvShow.Split('\n');
            string name = tvShowParts[0];
            int year = int.Parse(tvShowParts[1]);

            SearchContainer<SearchTv> searchContainer = await tmdbClient.SearchTvShowAsync(name, 0, true, year).ConfigureAwait(false);

            List<SearchTv> results = searchContainer.Results;

            if (results != null && results.Count > 0)
            {
                int tvShowId = results[0].Id;
                TMDbLib.Objects.TvShows.TvShow tvShowTmdb = await tmdbClient.GetTvShowAsync(tvShowId);
                if (tvShowTmdb != null)
                {
                    TvShowResponse tvShowResponse = new TvShowResponse()
                    {
                        Id = tvShowId.ToString(),
                        Genres = tvShowTmdb.Genres,
                        Poster = tvShowTmdb.PosterPath,
                        OriginCountry = tvShowTmdb.OriginCountry[0],
                        OverView = tvShowTmdb.Overview,
                        FirstAirDate = tvShowTmdb.FirstAirDate,
                        TagLine = tvShowTmdb.Tagline,
                        Title = tvShowTmdb.Name,
                        OriginalName = tvShowTmdb.OriginalName,
                        NumberOfSeasons = tvShowTmdb.NumberOfSeasons,
                    };
                    return tvShowResponse;
                }
            }
            return null;
        }

        private string ConvertToEasyName(Movie movie)
        {
            return $"{movie.Title} ({movie.ReleaseDate.Value.Year})";
        }

        private string ConvertToEasyName(TvShow tvShow)
        {
            return $"{tvShow.Title} ({tvShow.FirstAirDate.Value.Year})";
        }
    }
}
