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
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Movie = MediaVoyager.Models.Movie;
using TvShow = MediaVoyager.Models.TvShow;

namespace MediaVoyager.Services
{
    public class MediaRecommendationService : IMediaRecommendationService
    {
        private static readonly Regex RecommendationWithParenthesizedYearRegex = new(
            @"^(?<name>.+?)\s*\((?<year>\d{4})\)\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex RecommendationWithSeparatedYearRegex = new(
            @"^(?<name>.+?)\s*[-–,:]\s*(?<year>\d{4})\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex RecommendationYearRegex = new(
            @"\b(?<year>(18|19|20)\d{2})\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly IUserMoviesRepository userMoviesRepository;
        private readonly IUserTvRepository userTvRepository;
        private readonly string tmdbApiKey;
        private readonly ITmdbCacheService tmdbCacheService;
        private readonly IRecommendationClientResolver recommendationClientResolver;
        private readonly IRecommendationProviderService recommendationProviderService;
        private readonly IOmdbClient omdbClient;
        private readonly IHttpContextAccessor httpContextAccessor;

        public MediaRecommendationService(
            ISecretService secretService,
            IUserMoviesRepository userMoviesRepository,
            IUserTvRepository userTvRepository,
            ITmdbCacheService tmdbCacheService,
            IRecommendationClientResolver recommendationClientResolver,
            IRecommendationProviderService recommendationProviderService,
            IOmdbClient omdbClient,
            IHttpContextAccessor httpContextAccessor)
        {
            this.tmdbApiKey = secretService.GetSecretValue("tmdb_api_key");
            this.userMoviesRepository = userMoviesRepository;
            this.userTvRepository = userTvRepository;
            this.tmdbCacheService = tmdbCacheService;
            this.recommendationClientResolver = recommendationClientResolver;
            this.recommendationProviderService = recommendationProviderService;
            this.omdbClient = omdbClient;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<MovieResponse> GetMovieRecommendationForUser(string userId, double temp = 0.9, int recurCount = 0)
        {
            Log($"[MediaRec][Movie] Start GetMovieRecommendationForUser userId={userId}");
            try
            {
                TMDbClient tmdbClient = new TMDbClient(tmdbApiKey);
                Log("[MediaRec][Movie] TMDbClient created");

                UserMovies userMovies = await this.userMoviesRepository.GetUserMovies(userId).ConfigureAwait(false);
                Log(userMovies == null
                    ? "[MediaRec][Movie] No userMovies found"
                    : $"[MediaRec][Movie] Loaded userMovies favourites={userMovies.favouriteMovies?.Count ??0} watchHistory={userMovies.watchHistory?.Count ??0}");

                if (userMovies == null)
                {
                    Log("[MediaRec][Movie] Returning null because userMovies is null");
                    return null;
                }

                List<string> favouriteMovies = userMovies.favouriteMovies.Select(x => ConvertToEasyName(x)).ToList<string>();
                List<string> watchHistory = userMovies.watchHistory.Select(x => ConvertToEasyName(x)).ToList<string>();
                Log($"[MediaRec][Movie] favouriteMovies(count={favouriteMovies.Count}) sample=[{string.Join(", ", favouriteMovies.Take(3))}]...");
                Log($"[MediaRec][Movie] watchHistory(count={watchHistory.Count}) sample=[{string.Join(", ", watchHistory.Take(3))}]...");

                IRecommendationClient recClient = GetRecommendationClient();

                string movie = null;
                string name = null;
                int year = 0;
                for (double temperature = temp; temperature <= 2.0; temperature += 0.2)
                {
                    movie = await recClient.GetMovieRecommendationAsync(favouriteMovies, watchHistory, temperature);
                    Log($"[MediaRec][Movie] Recommendation (temp={temperature:F1}): '{movie}'");
                    if (string.IsNullOrEmpty(movie))
                    {
                        Log($"[MediaRec][Movie] Empty recommendation at temp={temperature:F1}, retrying with higher temperature");
                        continue;
                    }

                    if (!TryParseRecommendation(movie, out name, out year))
                    {
                        Log($"[MediaRec][Movie] Could not parse recommendation '{movie}', retrying with higher temperature");
                        movie = null;
                        continue;
                    }

                    Log($"[MediaRec][Movie] Parsed recommendation name='{name}', year={year}");

                    // Check if the recommended movie already exists in watch history
                    if (IsMovieInWatchHistory(userMovies.watchHistory, name, year))
                    {
                        Log($"[MediaRec][Movie] Movie '{name}' ({year}) already in watch history, retrying with higher temperature");
                        movie = null;
                        continue;
                    }

                    break;
                }

                if (string.IsNullOrEmpty(movie))
                {
                    Log("[MediaRec][Movie] Returning null because recommendation is empty after all retries");
                    return null;
                }

                Log($"[MediaRec][Movie] Searching TMDb for movie name='{name}', year={year}");
                SearchContainer<SearchMovie> searchContainer = await tmdbClient.SearchMovieAsync(name,0, false, year).ConfigureAwait(false);
                List<SearchMovie> results = searchContainer.Results;
                Log($"[MediaRec][Movie] Search results with year: count={(results?.Count ??0)}");

                bool retryWithoutYear = false;

                if (results == null || results.Count ==0)
                {
                    Log("[MediaRec][Movie] No results with year filter, retrying without year");
                    searchContainer = await tmdbClient.SearchMovieAsync(name,0).ConfigureAwait(false);
                    results = searchContainer.Results;
                    retryWithoutYear = true;
                    Log($"[MediaRec][Movie] Search results without year: count={(results?.Count ??0)}");
                }

                if (results != null && results.Count >0)
                {
                    // Compare top two results for popularity and use the more popular one
                    SearchMovie selectedMovie = results[0];
                    if (results.Count > 1 && results[1].Popularity > results[0].Popularity && retryWithoutYear)
                    {
                        if (results[1].Title.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            selectedMovie = results[1];
                            Log($"[MediaRec][Movie] Selected second result (more popular): movieId={selectedMovie.Id}, popularity={selectedMovie.Popularity} vs first result popularity={results[0].Popularity}");
                        }
                    }
                    else
                    {
                        Log($"[MediaRec][Movie] Using top result movieId={selectedMovie.Id}, popularity={selectedMovie.Popularity}");
                    }

                    int movieId = selectedMovie.Id;

                    if (this.IsMovieIdInWatchHistory(userMovies.watchHistory, movieId.ToString()) && recurCount < 3)
                    {
                        return await GetMovieRecommendationForUser(userId, temp + 0.1, recurCount + 1);
                    }

                    TMDbLib.Objects.Movies.Movie movieTmdb = await this.tmdbCacheService.GetMovieAsync(movieId);
                    Log(movieTmdb == null
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
                        Log($"[MediaRec][Movie] Returning response Id={movieResponse.Id} Title='{movieResponse.Title}' Poster='{movieResponse.Poster}'");
                        return movieResponse;
                    }
                    Log("[MediaRec][Movie] Returning null because movie details were null");
                }
                else
                {
                    if (recurCount < 4)
                    {
                        return await GetMovieRecommendationForUser(userId, temp + 0.1, recurCount + 1);
                    }
                    Log("[MediaRec][Movie] Returning null because no search results were found");
                }
                return null;
            }
            catch (Exception ex)
            {
                Log($"[MediaRec][Movie] Exception: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
            finally
            {
                Log($"[MediaRec][Movie] End GetMovieRecommendationForUser userId={userId}");
            }
        }

        public async Task<TvShowResponse> GetTvShowRecommendationForUser(string userId, double temp = 0.9, int recurCount = 0)
        {
            Log($"[MediaRec][TV] Start GetTvShowRecommendationForUser userId={userId}");
            try
            {
                TMDbClient tmdbClient = new TMDbClient(tmdbApiKey);
                Log("[MediaRec][TV] TMDbClient created");

                UserTv userTvShows = await this.userTvRepository.GetUserTv(userId).ConfigureAwait(false);
                Log(userTvShows == null
                    ? "[MediaRec][TV] No userTvShows found"
                    : $"[MediaRec][TV] Loaded userTvShows favourites={userTvShows.favouriteTv?.Count ??0} watchHistory={userTvShows.watchHistory?.Count ??0}");

                if (userTvShows == null)
                {
                    Log("[MediaRec][TV] Returning null because userTvShows is null");
                    return null;
                }

                List<string> favouriteTvShows = userTvShows.favouriteTv.Select(x => ConvertToEasyName(x)).ToList<string>();
                List<string> watchHistory = userTvShows.watchHistory.Select(x => ConvertToEasyName(x)).ToList<string>();
                Log($"[MediaRec][TV] favouriteTvShows(count={favouriteTvShows.Count}) sample=[{string.Join(", ", favouriteTvShows.Take(3))}]...");
                Log($"[MediaRec][TV] watchHistory(count={watchHistory.Count}) sample=[{string.Join(", ", watchHistory.Take(3))}]...");

                IRecommendationClient recClient = GetRecommendationClient();

                string tvShow = null;
                string name = null;
                int year = 0;
                for (double temperature = temp; temperature <= 2.0; temperature += 0.2)
                {
                    tvShow = await recClient.GetTvShowRecommendationAsync(favouriteTvShows, watchHistory, temperature);
                    Log($"[MediaRec][TV] Recommendation (temp={temperature:F1}): '{tvShow}'");
                    if (string.IsNullOrEmpty(tvShow))
                    {
                        Log($"[MediaRec][TV] Empty recommendation at temp={temperature:F1}, retrying with higher temperature");
                        continue;
                    }

                    if (!TryParseRecommendation(tvShow, out name, out year))
                    {
                        Log($"[MediaRec][TV] Could not parse recommendation '{tvShow}', retrying with higher temperature");
                        tvShow = null;
                        continue;
                    }

                    Log($"[MediaRec][TV] Parsed recommendation name='{name}', year={year}");

                    // Check if the recommended TV show already exists in watch history
                    if (IsTvShowInWatchHistory(userTvShows.watchHistory, name, year))
                    {
                        Log($"[MediaRec][TV] TV show '{name}' ({year}) already in watch history, retrying with higher temperature");
                        tvShow = null;
                        continue;
                    }

                    break;
                }

                if (string.IsNullOrEmpty(tvShow))
                {
                    Log("[MediaRec][TV] Returning null because recommendation is empty after all retries");
                    return null;
                }

                Log($"[MediaRec][TV] Searching TMDb for TV show name='{name}', year={year}");
                SearchContainer<SearchTv> searchContainer = await tmdbClient.SearchTvShowAsync(name,0, false, year).ConfigureAwait(false);

                List<SearchTv> results = searchContainer.Results;

                bool retryWithoutYear = false;
                Log($"[MediaRec][TV] Search results with year: count={(results?.Count ??0)}");
                if (results == null || results.Count ==0)
                {
                    Log("[MediaRec][TV] No results with year filter, retrying without year");
                    searchContainer = await tmdbClient.SearchTvShowAsync(name,0).ConfigureAwait(false);
                    results = searchContainer.Results;
                    retryWithoutYear = true;
                    Log($"[MediaRec][TV] Search results without year: count={(results?.Count ??0)}");
                }

                if (results != null && results.Count >0)
                {
                    // Compare top two results for popularity and use the more popular one
                    SearchTv selectedTvShow = results[0];
                    if (results.Count > 1 && results[1].Popularity > results[0].Popularity && retryWithoutYear)
                    {
                        if (results[1].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            selectedTvShow = results[1];
                            Log($"[MediaRec][TV] Selected second result (more popular): tvShowId={selectedTvShow.Id}, popularity={selectedTvShow.Popularity} vs first result popularity={results[0].Popularity}");
                        }
                    }
                    else
                    {
                        Log($"[MediaRec][TV] Using top result tvShowId={selectedTvShow.Id}, popularity={selectedTvShow.Popularity}");
                    }

                    int tvShowId = selectedTvShow.Id;
                    Log($"[MediaRec][TV] Using top result tvShowId={tvShowId}");

                    if (this.IsTvShowIdInWatchHistory(userTvShows.watchHistory, tvShowId.ToString()) && recurCount < 3)
                    {
                        return await GetTvShowRecommendationForUser(userId, temp + 0.1, recurCount+1);
                    }

                    Task<TMDbLib.Objects.TvShows.TvShow> tvShowTask = this.tmdbCacheService.GetTvShowAsync(tvShowId);
                    Task<ExternalIdsTvShow> externalIdsTask = tmdbClient.GetTvShowExternalIdsAsync(tvShowId);

                    await Task.WhenAll(tvShowTask, externalIdsTask).ConfigureAwait(false);

                    TMDbLib.Objects.TvShows.TvShow tvShowTmdb = await tvShowTask.ConfigureAwait(false);

                    ExternalIdsTvShow externalIds = await externalIdsTask.ConfigureAwait(false);

                    Log(tvShowTmdb == null
                        ? "[MediaRec][TV] tmdbCacheService returned null for TV show"
                        : $"[MediaRec][TV] Loaded TV details: name='{tvShowTmdb.Name}', firstAirDate={tvShowTmdb.FirstAirDate}, genres={(tvShowTmdb.Genres == null ?0 : tvShowTmdb.Genres.Count)}");

                    string imdbId = externalIds?.ImdbId;
                    Log(string.IsNullOrWhiteSpace(imdbId)
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
                            NumberOfSeasons = tvShowTmdb.NumberOfSeasons,
                            ImdbRating = await imdbRatingTask.ConfigureAwait(false)
                        };
                        Log($"[MediaRec][TV] Returning response Id={tvShowResponse.Id} Title='{tvShowResponse.Title}' Poster='{tvShowResponse.Poster}'");
                        return tvShowResponse;
                    }
                    Log("[MediaRec][TV] Returning null because TV details were null");
                }
                else
                {
                    if (recurCount < 4)
                    {
                        return await GetTvShowRecommendationForUser(userId, temp + 0.1, recurCount + 1);
                    }
                    Log("[MediaRec][TV] Returning null because no search results were found");
                }
                return null;
            }
            catch (Exception ex)
            {
                Log($"[MediaRec][TV] Exception: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
            finally
            {
                Log($"[MediaRec][TV] End GetTvShowRecommendationForUser userId={userId}");
            }
        }

        private void Log(string message)
        {
            Console.WriteLine(message);
            // Resolve scoped IRequestLogCollector from the current HTTP request context
            var requestLogCollector = httpContextAccessor.HttpContext?.RequestServices?.GetService<IRequestLogCollector>();
            requestLogCollector?.AddLog(message);
        }

        private IRecommendationClient GetRecommendationClient()
        {
            // Get provider from the in-memory provider service
            RecommendationProvider provider = this.recommendationProviderService.CurrentProvider;
            Log($"[MediaRec] Using recommendation provider: {provider}");
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

        private static bool TryParseRecommendation(string recommendation, out string name, out int year)
        {
            name = null;
            year = 0;

            if (string.IsNullOrWhiteSpace(recommendation))
            {
                return false;
            }

            string normalizedRecommendation = recommendation
                .Replace('\u202F', ' ')
                .Replace('\u00A0', ' ')
                .Trim();

            string[] lines = normalizedRecommendation
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(CleanRecommendationLine)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();

            if (lines.Length == 0)
            {
                return false;
            }

            if (TryParseTitleAndYearFromSingleLine(lines[0], out name, out year))
            {
                return true;
            }

            if (lines.Length > 1 && TryParseYear(lines[1], out year))
            {
                name = CleanRecommendationTitle(lines[0]);
                return !string.IsNullOrWhiteSpace(name);
            }

            if (TryParseTitleAndYearFromSingleLine(CleanRecommendationLine(normalizedRecommendation), out name, out year))
            {
                return true;
            }

            if (lines.Length > 1 && TryParseYear(lines[^1], out year))
            {
                name = CleanRecommendationTitle(string.Join(" ", lines.Take(lines.Length - 1)));
                return !string.IsNullOrWhiteSpace(name);
            }

            return false;
        }

        private static bool TryParseTitleAndYearFromSingleLine(string line, out string name, out int year)
        {
            name = null;
            year = 0;

            string cleanedLine = CleanRecommendationLine(line);
            Match match = RecommendationWithParenthesizedYearRegex.Match(cleanedLine);
            if (!match.Success)
            {
                match = RecommendationWithSeparatedYearRegex.Match(cleanedLine);
            }

            if (!match.Success || !int.TryParse(match.Groups["year"].Value, out year))
            {
                return false;
            }

            name = CleanRecommendationTitle(match.Groups["name"].Value);
            return !string.IsNullOrWhiteSpace(name);
        }

        private static bool TryParseYear(string rawYear, out int year)
        {
            year = 0;
            if (string.IsNullOrWhiteSpace(rawYear))
            {
                return false;
            }

            Match yearMatch = RecommendationYearRegex.Match(CleanRecommendationLine(rawYear));
            return yearMatch.Success && int.TryParse(yearMatch.Groups["year"].Value, out year);
        }

        private static string CleanRecommendationTitle(string value)
        {
            return CleanRecommendationLine(value).Trim('(', ')', '[', ']');
        }

        private static string CleanRecommendationLine(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string cleaned = value
                .Replace('\u202F', ' ')
                .Replace('\u00A0', ' ')
                .Trim()
                .Trim('"', '\'', '`')
                .TrimStart('-', '*', '•')
                .Trim();

            foreach (string prefix in new[]
                     {
                         "Title:",
                         "Movie:",
                         "Show:",
                         "TV Show:",
                         "Name:",
                         "Year:",
                         "Release Year:",
                         "First Air Year:"
                     })
            {
                if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned[prefix.Length..].Trim();
                    break;
                }
            }

            return cleaned;
        }

        private bool IsMovieInWatchHistory(IEnumerable<Movie> watchHistory, string name, int year)
        {
            return watchHistory.Any(x => string.Equals(x.Title, name, StringComparison.OrdinalIgnoreCase) && x.ReleaseDate.HasValue && x.ReleaseDate.Value.Year == year);
        }

        private bool IsMovieIdInWatchHistory(IEnumerable<Movie> watchHistory, string id)
        {
            return watchHistory.Any(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsTvShowIdInWatchHistory(IEnumerable<TvShow> watchHistory, string id)
        {
            return watchHistory.Any(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsTvShowInWatchHistory(IEnumerable<TvShow> watchHistory, string name, int year)
        {
            return watchHistory.Any(x => string.Equals(x.Title, name, StringComparison.OrdinalIgnoreCase) && x.FirstAirDate.HasValue && x.FirstAirDate.Value.Year == year);
        }
    }
}
