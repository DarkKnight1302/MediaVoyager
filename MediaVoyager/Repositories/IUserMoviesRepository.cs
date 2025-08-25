using MediaVoyager.Entities;
using MediaVoyager.Models;

namespace MediaVoyager.Repositories
{
    public interface IUserMoviesRepository
    {
        public Task CreateUserMovies(string userId, List<Movie> favourites, List<Movie> watchHistory);

        public Task AddFavourites(string userId, List<Movie> favourites);

        public Task AddWatchHistory(string userId, List<Movie> watchHistory);

        public Task<UserMovies> GetUserMovies(string userId);
    }
}
