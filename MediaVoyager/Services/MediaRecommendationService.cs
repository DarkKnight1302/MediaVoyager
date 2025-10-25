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
        private readonly ITmdbCacheService tmdbCacheService;

        public MediaRecommendationService(ISecretService secretService,
            IUserMoviesRepository userMoviesRepository,
            IGeminiRecommendationClient geminiRecommendationClient,
            IUserTvRepository userTvRepository,
            ITmdbCacheService tmdbCacheService)
        {
            this.tmdbApiKey = secretService.GetSecretValue("tmdb_api_key");
            this.userMoviesRepository = userMoviesRepository;
            this.geminiRecommendationClient = geminiRecommendationClient;
            this.userTvRepository = userTvRepository;
            this.tmdbCacheService = tmdbCacheService;
        }

        public async Task<MovieResponse> GetMovieRecommendationForUser(string userId)
        {
            Console.WriteLine($"[MediaRec][Movie] Start GetMovieRecommendationForUser userId={userId}");
            try
            {
                TMDbClient tmdbClient = new TMDbClient(tmdbApiKey);
                Console.WriteLine("[MediaRec][Movie] TMDbClient created");

                UserMovies userMovies = await this.userMoviesRepository.GetUserMovies(userId).ConfigureAwait(false);
                Console.WriteLine(userMovies == null
                    ? "[MediaRec][Movie] No userMovies found"
                    : $"[MediaRec][Movie] Loaded userMovies favourites={userMovies.favouriteMovies?.Count ??0} watchHistory={userMovies.watchHistory?.Count ??0}");

                if (userMovies == null)
                {
                    Console.WriteLine("[MediaRec][Movie] Returning null because userMovies is null");
                    return null;
                }

                List<string> favouriteMovies = userMovies.favouriteMovies.Select(x => ConvertToEasyName(x)).ToList<string>();
                List<string> watchHistory = userMovies.watchHistory.Select(x => ConvertToEasyName(x)).ToList<string>();
                Console.WriteLine($"[MediaRec][Movie] favouriteMovies(count={favouriteMovies.Count}) sample=[{string.Join(", ", favouriteMovies.Take(3))}]...");
                Console.WriteLine($"[MediaRec][Movie] watchHistory(count={watchHistory.Count}) sample=[{string.Join(", ", watchHistory.Take(3))}]...");

                string movie = await this.geminiRecommendationClient.GetMovieRecommendationAsync(favouriteMovies, watchHistory,1);
                Console.WriteLine($"[MediaRec][Movie] Gemini recommendation (temp=1): '{movie}'");
                if (string.IsNullOrEmpty(movie))
                {
                    Console.WriteLine("[MediaRec][Movie] Empty recommendation at temp=1, retrying with temp=2");
                    movie = await this.geminiRecommendationClient.GetMovieRecommendationAsync(favouriteMovies, watchHistory,2);
                    Console.WriteLine($"[MediaRec][Movie] Gemini recommendation (temp=2): '{movie}'");
                }
                if (string.IsNullOrEmpty(movie))
                {
                    Console.WriteLine("[MediaRec][Movie] Returning null because recommendation is empty after retries");
                    return null;
                }

                string[] movieParts = movie.Split('\n');
                string name = movieParts[0];
                int year = int.Parse(movieParts[1]);
                Console.WriteLine($"[MediaRec][Movie] Parsed recommendation name='{name}', year={year}");

                Console.WriteLine($"[MediaRec][Movie] Searching TMDb for movie name='{name}', year={year}");
                SearchContainer<SearchMovie> searchContainer = await tmdbClient.SearchMovieAsync(name,0, false, year).ConfigureAwait(false);
                List<SearchMovie> results = searchContainer.Results;
                Console.WriteLine($"[MediaRec][Movie] Search results with year: count={(results?.Count ??0)}");

                if (results == null || results.Count ==0)
                {
                    Console.WriteLine("[MediaRec][Movie] No results with year filter, retrying without year");
                    searchContainer = await tmdbClient.SearchMovieAsync(name,0).ConfigureAwait(false);
                    results = searchContainer.Results;
                    Console.WriteLine($"[MediaRec][Movie] Search results without year: count={(results?.Count ??0)}");
                }

                if (results != null && results.Count >0)
                {
                    int movieId = results[0].Id;
                    Console.WriteLine($"[MediaRec][Movie] Using top result movieId={movieId}");
                    TMDbLib.Objects.Movies.Movie movieTmdb = await this.tmdbCacheService.GetMovieAsync(movieId);
                    Console.WriteLine(movieTmdb == null
                        ? "[MediaRec][Movie] tmdbCacheService returned null for movie"
                        : $"[MediaRec][Movie] Loaded movie details: title='{movieTmdb.Title}', releaseDate={movieTmdb.ReleaseDate}, genres={(movieTmdb.Genres == null ?0 : movieTmdb.Genres.Count)}");

                    if (movieTmdb != null)
                    {
                        MovieResponse movieResponse = new MovieResponse()
                        {
                            Id = movieId.ToString(),
                            Genres = movieTmdb.Genres,
                            Poster = movieTmdb.PosterPath,
                            OriginCountry = movieTmdb.ProductionCountries != null && movieTmdb.ProductionCountries.Count >0 ? movieTmdb.ProductionCountries[0].Name : string.Empty,
                            OverView = movieTmdb.Overview,
                            ReleaseDate = movieTmdb.ReleaseDate,
                            TagLine = movieTmdb.Tagline,
                            Title = movieTmdb.Title
                        };
                        Console.WriteLine($"[MediaRec][Movie] Returning response Id={movieResponse.Id} Title='{movieResponse.Title}' Poster='{movieResponse.Poster}'");
                        return movieResponse;
                    }
                    Console.WriteLine("[MediaRec][Movie] Returning null because movie details were null");
                }
                else
                {
                    Console.WriteLine("[MediaRec][Movie] Returning null because no search results were found");
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MediaRec][Movie] Exception: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
            finally
            {
                Console.WriteLine($"[MediaRec][Movie] End GetMovieRecommendationForUser userId={userId}");
            }
        }

        public async Task<TvShowResponse> GetTvShowRecommendationForUser(string userId)
        {
            Console.WriteLine($"[MediaRec][TV] Start GetTvShowRecommendationForUser userId={userId}");
            try
            {
                TMDbClient tmdbClient = new TMDbClient(tmdbApiKey);
                Console.WriteLine("[MediaRec][TV] TMDbClient created");

                UserTv userTvShows = await this.userTvRepository.GetUserTv(userId).ConfigureAwait(false);
                Console.WriteLine(userTvShows == null
                    ? "[MediaRec][TV] No userTvShows found"
                    : $"[MediaRec][TV] Loaded userTvShows favourites={userTvShows.favouriteTv?.Count ??0} watchHistory={userTvShows.watchHistory?.Count ??0}");

                if (userTvShows == null)
                {
                    Console.WriteLine("[MediaRec][TV] Returning null because userTvShows is null");
                    return null;
                }

                List<string> favouriteTvShows = userTvShows.favouriteTv.Select(x => ConvertToEasyName(x)).ToList<string>();
                List<string> watchHistory = userTvShows.watchHistory.Select(x => ConvertToEasyName(x)).ToList<string>();
                Console.WriteLine($"[MediaRec][TV] favouriteTvShows(count={favouriteTvShows.Count}) sample=[{string.Join(", ", favouriteTvShows.Take(3))}]...");
                Console.WriteLine($"[MediaRec][TV] watchHistory(count={watchHistory.Count}) sample=[{string.Join(", ", watchHistory.Take(3))}]...");

                string tvShow = await this.geminiRecommendationClient.GetTvShowRecommendationAsync(favouriteTvShows, watchHistory,1);
                Console.WriteLine($"[MediaRec][TV] Gemini recommendation (temp=1): '{tvShow}'");
                if (string.IsNullOrEmpty(tvShow))
                {
                    Console.WriteLine("[MediaRec][TV] Empty recommendation at temp=1, retrying with temp=2");
                    tvShow = await this.geminiRecommendationClient.GetTvShowRecommendationAsync(favouriteTvShows, watchHistory,2);
                    Console.WriteLine($"[MediaRec][TV] Gemini recommendation (temp=2): '{tvShow}'");
                }
                if (string.IsNullOrEmpty(tvShow))
                {
                    Console.WriteLine("[MediaRec][TV] Returning null because recommendation is empty after retries");
                    return null;
                }

                string[] tvShowParts = tvShow.Split('\n');
                string name = tvShowParts[0];
                int year = int.Parse(tvShowParts[1]);
                Console.WriteLine($"[MediaRec][TV] Parsed recommendation name='{name}', year={year}");

                Console.WriteLine($"[MediaRec][TV] Searching TMDb for TV show name='{name}', year={year}");
                SearchContainer<SearchTv> searchContainer = await tmdbClient.SearchTvShowAsync(name,0, false, year).ConfigureAwait(false);

                List<SearchTv> results = searchContainer.Results;
                Console.WriteLine($"[MediaRec][TV] Search results with year: count={(results?.Count ??0)}");
                if (results == null || results.Count ==0)
                {
                    Console.WriteLine("[MediaRec][TV] No results with year filter, retrying without year");
                    searchContainer = await tmdbClient.SearchTvShowAsync(name,0).ConfigureAwait(false);
                    results = searchContainer.Results;
                    Console.WriteLine($"[MediaRec][TV] Search results without year: count={(results?.Count ??0)}");
                }

                if (results != null && results.Count >0)
                {
                    int tvShowId = results[0].Id;
                    Console.WriteLine($"[MediaRec][TV] Using top result tvShowId={tvShowId}");
                    TMDbLib.Objects.TvShows.TvShow tvShowTmdb = await this.tmdbCacheService.GetTvShowAsync(tvShowId);
                    Console.WriteLine(tvShowTmdb == null
                        ? "[MediaRec][TV] tmdbCacheService returned null for TV show"
                        : $"[MediaRec][TV] Loaded TV details: name='{tvShowTmdb.Name}', firstAirDate={tvShowTmdb.FirstAirDate}, genres={(tvShowTmdb.Genres == null ?0 : tvShowTmdb.Genres.Count)}");

                    if (tvShowTmdb != null)
                    {
                        TvShowResponse tvShowResponse = new TvShowResponse()
                        {
                            Id = tvShowId.ToString(),
                            Genres = tvShowTmdb.Genres,
                            Poster = tvShowTmdb.PosterPath,
                            OriginCountry = tvShowTmdb.OriginCountry != null && tvShowTmdb.OriginCountry.Count >0 ? tvShowTmdb.OriginCountry[0] : string.Empty,
                            OverView = tvShowTmdb.Overview,
                            FirstAirDate = tvShowTmdb.FirstAirDate,
                            TagLine = tvShowTmdb.Tagline,
                            Title = tvShowTmdb.Name,
                            OriginalName = tvShowTmdb.OriginalName,
                            NumberOfSeasons = tvShowTmdb.NumberOfSeasons,
                        };
                        Console.WriteLine($"[MediaRec][TV] Returning response Id={tvShowResponse.Id} Title='{tvShowResponse.Title}' Poster='{tvShowResponse.Poster}'");
                        return tvShowResponse;
                    }
                    Console.WriteLine("[MediaRec][TV] Returning null because TV details were null");
                }
                else
                {
                    Console.WriteLine("[MediaRec][TV] Returning null because no search results were found");
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MediaRec][TV] Exception: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
            finally
            {
                Console.WriteLine($"[MediaRec][TV] End GetTvShowRecommendationForUser userId={userId}");
            }
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
