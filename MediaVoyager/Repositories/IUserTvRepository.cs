using MediaVoyager.Entities;
using MediaVoyager.Models;

namespace MediaVoyager.Repositories
{
    public interface IUserTvRepository
    {
        public Task<UserTv> CreateUserTv(string userId, List<TvShow> favourites, List<TvShow> watchHistory);

        public Task AddFavourites(string userId, List<TvShow> favourites);

        public Task AddWatchHistory(string userId, List<TvShow> watchHistory);

        public Task<UserTv> GetUserTv(string userId);

        public Task UpsertUserTv(UserTv userTv);

        public Task RemoveFavourites(string userId, List<string> tvIds);
    }
}