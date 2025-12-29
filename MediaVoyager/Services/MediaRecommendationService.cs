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
        private readonly IUserTvRepository userTvRepository;
        private readonly string tmdbApiKey;
        private readonly ITmdbCacheService tmdbCacheService;
        private readonly IRecommendationClientResolver recommendationClientResolver;
        private readonly IRecommendationProviderService recommendationProviderService;
        private readonly IOmdbClient omdbClient;

        public MediaRecommendationService(
            ISecretService secretService,
            IUserMoviesRepository userMoviesRepository,
            IUserTvRepository userTvRepository,
            ITmdbCacheService tmdbCacheService,
            IRecommendationClientResolver recommendationClientResolver,
            IRecommendationProviderService recommendationProviderService,
            IOmdbClient omdbClient)
        {
            this.tmdbApiKey = secretService.GetSecretValue("tmdb_api_key");
            this.userMoviesRepository = userMoviesRepository;
            this.userTvRepository = userTvRepository;
            this.tmdbCacheService = tmdbCacheService;
            this.recommendationClientResolver = recommendationClientResolver;
            this.recommendationProviderService = recommendationProviderService;
            this.omdbClient = omdbClient;
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

                IRecommendationClient recClient = GetRecommendationClient();

                string movie = await recClient.GetMovieRecommendationAsync(favouriteMovies, watchHistory, 1);
                Console.WriteLine($"[MediaRec][Movie] Recommendation (temp=1): '{movie}'");
                if (string.IsNullOrEmpty(movie))
                {
                    Console.WriteLine("[MediaRec][Movie] Empty recommendation at temp=1, retrying with temp=2");
                    movie = await recClient.GetMovieRecommendationAsync(favouriteMovies, watchHistory, 2);
                    Console.WriteLine($"[MediaRec][Movie] Recommendation (temp=2): '{movie}'");
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
                            Title = movieTmdb.Title,
                            ImdbRating = await this.omdbClient.TryGetImdbRatingAsync(movieTmdb.ImdbId).ConfigureAwait(false)
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

                IRecommendationClient recClient = GetRecommendationClient();

                string tvShow = await recClient.GetTvShowRecommendationAsync(favouriteTvShows, watchHistory, 1);
                Console.WriteLine($"[MediaRec][TV] Recommendation (temp=1): '{tvShow}'");
                if (string.IsNullOrEmpty(tvShow))
                {
                    Console.WriteLine("[MediaRec][TV] Empty recommendation at temp=1, retrying with temp=2");
                    tvShow = await recClient.GetTvShowRecommendationAsync(favouriteTvShows, watchHistory, 2);
                    Console.WriteLine($"[MediaRec][TV] Recommendation (temp=2): '{tvShow}'");
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
                    Task<TMDbLib.Objects.TvShows.TvShow> tvShowTask = this.tmdbCacheService.GetTvShowAsync(tvShowId);
                    Task<ExternalIdsTvShow> externalIdsTask = tmdbClient.GetTvShowExternalIdsAsync(tvShowId);

                    await Task.WhenAll(tvShowTask, externalIdsTask).ConfigureAwait(false);

                    TMDbLib.Objects.TvShows.TvShow tvShowTmdb = await tvShowTask.ConfigureAwait(false);
                    ExternalIdsTvShow externalIds = await externalIdsTask.ConfigureAwait(false);

                    Console.WriteLine(tvShowTmdb == null
                        ? "[MediaRec][TV] tmdbCacheService returned null for TV show"
                        : $"[MediaRec][TV] Loaded TV details: name='{tvShowTmdb.Name}', firstAirDate={tvShowTmdb.FirstAirDate}, genres={(tvShowTmdb.Genres == null ?0 : tvShowTmdb.Genres.Count)}");

                    string imdbId = externalIds?.ImdbId;
                    Console.WriteLine(string.IsNullOrWhiteSpace(imdbId)
                        ? "[MediaRec][TV] External IDs did not contain an IMDb id"
                        : $"[MediaRec][TV] External IDs imdbId='{imdbId}'");

                    if (tvShowTmdb != null)
                    {
                        Task<string> imdbRatingTask = this.omdbClient.TryGetImdbRatingAsync(imdbId);

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
                            ImdbRating = await imdbRatingTask.ConfigureAwait(false)
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

        private IRecommendationClient GetRecommendationClient()
        {
            // Get provider from the in-memory provider service
            RecommendationProvider provider = this.recommendationProviderService.CurrentProvider;
            Console.WriteLine($"[MediaRec] Using recommendation provider: {provider}");
            return this.recommendationClientResolver.Resolve(provider);
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
