using MediaVoyager.Entities;

namespace MediaVoyager.Repositories
{
    public interface IUserRepository
    {
        public Task<User> CreateUser(User user);
        public Task<User> CreateUser(string id, string name, bool isGoogleLogin, string email, string passwordHash);
        public Task<User> GetUser(string userId);
        public Task<User> GetUserByEmail(string email);
        public Task<User> UpdateUser(User user);
        
        // Watchlist methods
        public Task<User> AddMoviesToWatchlist(string userId, List<string> movieIds);
        public Task<User> RemoveMoviesFromWatchlist(string userId, List<string> movieIds);
        public Task<User> AddTvShowsToWatchlist(string userId, List<string> tvIds);
        public Task<User> RemoveTvShowsFromWatchlist(string userId, List<string> tvIds);
    }
}
