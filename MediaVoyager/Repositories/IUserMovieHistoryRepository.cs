using MediaVoyager.Entities;
using MediaVoyager.Models;

namespace MediaVoyager.Repositories
{
    public interface IUserMovieHistoryRepository
    {
        Task<UserMovieHistory> GetUserMovieHistory(string userId);

        Task<UserMovieHistory> CreateUserMovieHistory(string userId, List<Movie> movies);

        Task AddToHistory(string userId, List<Movie> movies);

        Task UpsertUserMovieHistory(UserMovieHistory history);

        Task RemoveFromHistory(string userId, List<string> movieIds);
    }
}
