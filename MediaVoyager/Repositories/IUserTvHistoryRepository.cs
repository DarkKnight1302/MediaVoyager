using MediaVoyager.Entities;
using MediaVoyager.Models;

namespace MediaVoyager.Repositories
{
    public interface IUserTvHistoryRepository
    {
        Task<UserTvHistory> GetUserTvHistory(string userId);

        Task<UserTvHistory> CreateUserTvHistory(string userId, List<TvShow> tvShows);

        Task AddToHistory(string userId, List<TvShow> tvShows);

        Task UpsertUserTvHistory(UserTvHistory history);

        Task RemoveFromHistory(string userId, List<string> tvIds);
    }
}
