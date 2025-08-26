using MediaVoyager.Models;
using TMDbLib.Objects.Search;

namespace MediaVoyager.Services.Interfaces
{
    public interface IUserMediaService
    {
        public Task AddMoviesToWatchHistory(string userId, List<SearchMovie> movie);

        public Task AddMoviesToFavourites(string userID, List<SearchMovie> movie);
    }
}
