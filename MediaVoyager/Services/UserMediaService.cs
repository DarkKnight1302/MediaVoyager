using MediaVoyager.Entities;
using MediaVoyager.Models;
using MediaVoyager.Repositories;
using MediaVoyager.Services.Interfaces;
using TMDbLib.Objects.Search;

namespace MediaVoyager.Services
{
    public class UserMediaService : IUserMediaService
    {
        private readonly IUserMoviesRepository userMoviesRepository;
        private readonly IUserTvRepository userTvRepository;

        public UserMediaService(IUserMoviesRepository userMoviesRepository, IUserTvRepository userTvRepository)
        {
            this.userMoviesRepository = userMoviesRepository;
            this.userTvRepository = userTvRepository;
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
