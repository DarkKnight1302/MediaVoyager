using MediaVoyager.Models;
using TMDbLib.Objects.Search;

namespace MediaVoyager.Services.Interfaces
{
    public interface IUserMediaService
    {
        public Task AddMoviesToWatchHistory(string userId, List<SearchMovie> movies);
        public Task AddMoviesToFavourites(string userId, List<SearchMovie> movies);
        public Task AddTvShowsToFavourites(string userId, List<SearchTv> tvShows);
        public Task AddTvShowsToWatchHistory(string userId, List<SearchTv> tvShows);
    }
}
