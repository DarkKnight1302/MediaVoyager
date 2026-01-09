using MediaVoyager.Entities;
using MediaVoyager.Models;
using MediaVoyager.Repositories;
using MediaVoyager.Services.Interfaces;
using TMDbLib.Objects.Search;
using TMDbLib.Client;
using NewHorizonLib.Services;

namespace MediaVoyager.Services
{
    public class UserMediaService : IUserMediaService
    {
        private readonly IUserMoviesRepository userMoviesRepository;
        private readonly IUserTvRepository userTvRepository;
        private readonly IUserRepository userRepository;
        private readonly ITmdbCacheService tmdbCacheService;
        private readonly IUserMovieHistoryRepository userMovieHistoryRepository;
        private readonly IUserTvHistoryRepository userTvHistoryRepository;

        public UserMediaService(IUserMoviesRepository userMoviesRepository, 
                               IUserTvRepository userTvRepository,
                               IUserRepository userRepository,
                               ISecretService secretService,
                               ITmdbCacheService tmdbCacheService,
                               IUserMovieHistoryRepository userMovieHistoryRepository,
                               IUserTvHistoryRepository userTvHistoryRepository)
        {
            this.userMoviesRepository = userMoviesRepository;
            this.userTvRepository = userTvRepository;
            this.userRepository = userRepository;
            this.tmdbCacheService = tmdbCacheService;
            this.userMovieHistoryRepository = userMovieHistoryRepository;
            this.userTvHistoryRepository = userTvHistoryRepository;
        }

        public async Task AddMoviesToFavourites(string userId, List<SearchMovie> movies)
        {
            UserMovies userMovies = await this.userMoviesRepository.GetUserMovies(userId).ConfigureAwait(false);
            List<Movie> favMovies = ConvertToMovieObject(movies);
            if (userMovies == null)
            {
                userMovies = await this.userMoviesRepository.CreateUserMovies(userId, favMovies, favMovies).ConfigureAwait(false);
                return;
            }
            userMovies.favouriteMovies.UnionWith(favMovies);
            userMovies.watchHistory.UnionWith(favMovies);
            await this.userMoviesRepository.UpsertUserMovies(userMovies);
        }

        public async Task AddMoviesToWatchHistory(string userId, List<SearchMovie> movies)
        {
            UserMovies userMovies = await this.userMoviesRepository.GetUserMovies(userId).ConfigureAwait(false);
            List<Movie> watchHistoryMovies = ConvertToMovieObject(movies);
            if (userMovies == null)
            {
                userMovies = await this.userMoviesRepository.CreateUserMovies(userId, new List<Movie>(), watchHistoryMovies).ConfigureAwait(false);
                return;
            }
            userMovies.watchHistory.UnionWith(watchHistoryMovies);
            await this.userMoviesRepository.UpsertUserMovies(userMovies);
        }

        public async Task AddTvShowsToFavourites(string userId, List<SearchTv> tvShows)
        {
            UserTv userTv = await this.userTvRepository.GetUserTv(userId).ConfigureAwait(false);
            List<TvShow> favTvShows = ConvertToTvShowObject(tvShows);
            if (userTv == null)
            {
                userTv = await this.userTvRepository.CreateUserTv(userId, favTvShows, favTvShows).ConfigureAwait(false);
                return;
            }
            userTv.favouriteTv.AddRange(favTvShows.Where(tv => !userTv.favouriteTv.Contains(tv)));
            userTv.watchHistory.AddRange(favTvShows.Where(tv => !userTv.watchHistory.Contains(tv)));
            await this.userTvRepository.UpsertUserTv(userTv);
        }

        public async Task AddTvShowsToWatchHistory(string userId, List<SearchTv> tvShows)
        {
            UserTv userTv = await this.userTvRepository.GetUserTv(userId).ConfigureAwait(false);
            List<TvShow> watchHistoryTvShows = ConvertToTvShowObject(tvShows);
            if (userTv == null)
            {
                userTv = await this.userTvRepository.CreateUserTv(userId, new List<TvShow>(), watchHistoryTvShows).ConfigureAwait(false);
                return;
            }
            userTv.watchHistory.AddRange(watchHistoryTvShows.Where(tv => !userTv.watchHistory.Contains(tv)));
            await this.userTvRepository.UpsertUserTv(userTv);
        }

        // Watchlist methods
        public async Task AddMoviesToWatchlist(string userId, List<string> movieIds)
        {
            await this.userRepository.AddMoviesToWatchlist(userId, movieIds);
        }

        public async Task RemoveMoviesFromWatchlist(string userId, List<string> movieIds)
        {
            // Fetch movie details before removing from watchlist to add to history
            var moviesToMoveToHistory = new List<Movie>();
            foreach (var movieId in movieIds)
            {
                try
                {
                    var movie = await this.tmdbCacheService.GetMovieAsync(int.Parse(movieId));
                    if (movie != null)
                    {
                        moviesToMoveToHistory.Add(new Movie
                        {
                            Id = movie.Id.ToString(),
                            Title = movie.Title,
                            ReleaseDate = movie.ReleaseDate,
                            Poster = movie.PosterPath,
                            Overview = movie.Overview
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching movie {movieId} for history: {ex.Message}");
                }
            }

            // Add to history
            if (moviesToMoveToHistory.Count > 0)
            {
                await this.userMovieHistoryRepository.AddToHistory(userId, moviesToMoveToHistory);
            }

            // Remove from watchlist
            await this.userRepository.RemoveMoviesFromWatchlist(userId, movieIds);
        }

        public async Task AddTvShowsToWatchlist(string userId, List<string> tvIds)
        {
            await this.userRepository.AddTvShowsToWatchlist(userId, tvIds);
        }

        public async Task RemoveTvShowsFromWatchlist(string userId, List<string> tvIds)
        {
            // Fetch TV show details before removing from watchlist to add to history
            var tvShowsToMoveToHistory = new List<TvShow>();
            foreach (var tvId in tvIds)
            {
                try
                {
                    var tvShow = await this.tmdbCacheService.GetTvShowAsync(int.Parse(tvId));
                    if (tvShow != null)
                    {
                        tvShowsToMoveToHistory.Add(new TvShow
                        {
                            Id = tvShow.Id.ToString(),
                            Title = tvShow.Name,
                            FirstAirDate = tvShow.FirstAirDate,
                            Poster = tvShow.PosterPath,
                            Overview = tvShow.Overview
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching TV show {tvId} for history: {ex.Message}");
                }
            }

            // Add to history
            if (tvShowsToMoveToHistory.Count > 0)
            {
                await this.userTvHistoryRepository.AddToHistory(userId, tvShowsToMoveToHistory);
            }

            // Remove from watchlist
            await this.userRepository.RemoveTvShowsFromWatchlist(userId, tvIds);
        }

        public async Task RemoveMoviesFromFavourites(string userId, List<string> movieIds)
        {
            await this.userMoviesRepository.RemoveFavourites(userId, movieIds);
        }

        public async Task RemoveTvShowsFromFavourites(string userId, List<string> tvIds)
        {
            await this.userTvRepository.RemoveFavourites(userId, tvIds);
        }

        public async Task<FavouriteMoviesResponse> GetUserFavouriteMovies(string userId)
        {
            var userMovies = await this.userMoviesRepository.GetUserMovies(userId);
            if (userMovies == null)
            {
                throw new ArgumentException($"User with id {userId} not found");
            }

            var response = new FavouriteMoviesResponse();

            // Fetch movies from TMDb (via cache wrapper)
            if (userMovies.favouriteMovies?.Any() == true)
            {
                var movieTasks = userMovies.favouriteMovies.Select(async favouriteMovie =>
                {
                    try
                    {
                        var movie = await this.tmdbCacheService.GetMovieAsync(int.Parse(favouriteMovie.Id));
                        if (movie != null)
                        {
                            return new Movie
                            {
                                Id = movie.Id.ToString(),
                                Title = movie.Title,
                                ReleaseDate = movie.ReleaseDate,
                                Poster = movie.PosterPath,
                                Overview = movie.Overview
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error and continue
                        Console.WriteLine($"Error fetching movie {favouriteMovie.Id}: {ex.Message}");
                    }
                    return null;
                });

                var movies = await Task.WhenAll(movieTasks);
                response.movies = movies.Where(m => m != null).ToList();
            }

            return response;
        }

        public async Task<WatchlistResponse> GetUserWatchlist(string userId)
        {
            var user = await this.userRepository.GetUser(userId);
            if (user == null)
            {
                throw new ArgumentException($"User with id {userId} not found");
            }

            var response = new WatchlistResponse();

            // Fetch movies from TMDb (via cache wrapper)
            if (user.movieWatchlist?.Any() == true)
            {
                var movieTasks = user.movieWatchlist.Select(async movieId =>
                {
                    try
                    {
                        var movie = await this.tmdbCacheService.GetMovieAsync(int.Parse(movieId));
                        if (movie != null)
                        {
                            return new Movie
                            {
                                Id = movie.Id.ToString(),
                                Title = movie.Title,
                                ReleaseDate = movie.ReleaseDate,
                                Poster = movie.PosterPath,
                                Overview = movie.Overview
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error and continue
                        Console.WriteLine($"Error fetching movie {movieId}: {ex.Message}");
                    }
                    return null;
                });

                var movies = await Task.WhenAll(movieTasks);
                response.movies = movies.Where(m => m != null).ToList();
            }

            // Fetch TV shows from TMDb (via cache wrapper)
            if (user.tvWatchlist?.Any() == true)
            {
                var tvTasks = user.tvWatchlist.Select(async tvId =>
                {
                    try
                    {
                        var tvShow = await this.tmdbCacheService.GetTvShowAsync(int.Parse(tvId));
                        if (tvShow != null)
                        {
                            return new TvShow
                            {
                                Id = tvShow.Id.ToString(),
                                Title = tvShow.Name,
                                FirstAirDate = tvShow.FirstAirDate,
                                Poster = tvShow.PosterPath,
                                Overview = tvShow.Overview
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error and continue
                        Console.WriteLine($"Error fetching TV show {tvId}: {ex.Message}");
                    }
                    return null;
                });

                var tvShows = await Task.WhenAll(tvTasks);
                response.tvShows = tvShows.Where(tv => tv != null).ToList();
            }

            return response;
        }

        public async Task<FavouriteTvShowsResponse> GetUserFavouriteTvShows(string userId)
        {
            var userTv = await this.userTvRepository.GetUserTv(userId);
            if (userTv == null)
            {
                throw new ArgumentException($"User with id {userId} not found");
            }

            var response = new FavouriteTvShowsResponse();

            // Fetch TV shows from TMDb (via cache wrapper)
            if (userTv.favouriteTv?.Any() == true)
            {
                var tvTasks = userTv.favouriteTv.Select(async favouriteTv =>
                {
                    try
                    {
                        var tvShow = await this.tmdbCacheService.GetTvShowAsync(int.Parse(favouriteTv.Id));
                        if (tvShow != null)
                        {
                            return new TvShow
                            {
                                Id = tvShow.Id.ToString(),
                                Title = tvShow.Name,
                                FirstAirDate = tvShow.FirstAirDate,
                                Poster = tvShow.PosterPath,
                                Overview = tvShow.Overview
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error and continue
                        Console.WriteLine($"Error fetching TV show {favouriteTv.Id}: {ex.Message}");
                    }
                    return null;
                });

                var tvShows = await Task.WhenAll(tvTasks);
                response.tvShows = tvShows.Where(tv => tv != null).ToList();
            }

            return response;
        }

        public async Task<HistoryResponse> GetUserHistory(string userId)
        {
            var response = new HistoryResponse();

            // Get movie history
            var movieHistory = await this.userMovieHistoryRepository.GetUserMovieHistory(userId);
            if (movieHistory?.movies?.Any() == true)
            {
                var movieTasks = movieHistory.movies.Select(async historyMovie =>
                {
                    try
                    {
                        var movie = await this.tmdbCacheService.GetMovieAsync(int.Parse(historyMovie.Id));
                        if (movie != null)
                        {
                            return new Movie
                            {
                                Id = movie.Id.ToString(),
                                Title = movie.Title,
                                ReleaseDate = movie.ReleaseDate,
                                Poster = movie.PosterPath,
                                Overview = movie.Overview
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching movie {historyMovie.Id}: {ex.Message}");
                    }
                    return null;
                });

                var movies = await Task.WhenAll(movieTasks);
                response.movies = movies.Where(m => m != null).ToList();
            }

            // Get TV show history
            var tvHistory = await this.userTvHistoryRepository.GetUserTvHistory(userId);
            if (tvHistory?.tvShows?.Any() == true)
            {
                var tvTasks = tvHistory.tvShows.Select(async historyTv =>
                {
                    try
                    {
                        var tvShow = await this.tmdbCacheService.GetTvShowAsync(int.Parse(historyTv.Id));
                        if (tvShow != null)
                        {
                            return new TvShow
                            {
                                Id = tvShow.Id.ToString(),
                                Title = tvShow.Name,
                                FirstAirDate = tvShow.FirstAirDate,
                                Poster = tvShow.PosterPath,
                                Overview = tvShow.Overview
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching TV show {historyTv.Id}: {ex.Message}");
                    }
                    return null;
                });

                var tvShows = await Task.WhenAll(tvTasks);
                response.tvShows = tvShows.Where(tv => tv != null).ToList();
            }

            return response;
        }

        private List<Movie> ConvertToMovieObject(List<SearchMovie> movies)
        {
            return movies.Select(x =>
                new Movie()
                {
                    Id = x.Id.ToString(),
                    ReleaseDate = x.ReleaseDate,
                    Title = x.Title
                }).ToList();
        }

        private List<TvShow> ConvertToTvShowObject(List<SearchTv> tvShows)
        {
            return tvShows.Select(x =>
                new TvShow()
                {
                    Id = x.Id.ToString(),
                    Title = x.Name,
                    FirstAirDate = x.FirstAirDate
                }).ToList();
        }
    }
}
