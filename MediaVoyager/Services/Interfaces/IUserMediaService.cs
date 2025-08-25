using MediaVoyager.Models;

namespace MediaVoyager.Services.Interfaces
{
    public interface IUserMediaService
    {
        public Task AddMovieToWatchHistory(string userId, Movie movie);

        public Task AddMovieToFavourites(string userID, Movie movie);
    }
}
