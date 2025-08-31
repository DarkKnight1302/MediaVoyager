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

        public UserMediaService(IUserMoviesRepository userMoviesRepository)
        {
            this.userMoviesRepository = userMoviesRepository;
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

        public Task AddMoviesToWatchHistory(string userId, List<SearchMovie> movies)
        {
            throw new NotImplementedException();
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
    }
}
