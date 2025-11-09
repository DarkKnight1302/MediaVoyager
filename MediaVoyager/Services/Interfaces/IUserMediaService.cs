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
        
        // Watchlist methods
        public Task AddMoviesToWatchlist(string userId, List<string> movieIds);
        public Task RemoveMoviesFromWatchlist(string userId, List<string> movieIds);
        public Task AddTvShowsToWatchlist(string userId, List<string> tvIds);
        public Task RemoveTvShowsFromWatchlist(string userId, List<string> tvIds);
        public Task<WatchlistResponse> GetUserWatchlist(string userId);
        
        // Favourites methods
        public Task RemoveMoviesFromFavourites(string userId, List<string> movieIds);
        public Task<FavouriteMoviesResponse> GetUserFavouriteMovies(string userId);
        public Task RemoveTvShowsFromFavourites(string userId, List<string> tvIds);
        public Task<FavouriteTvShowsResponse> GetUserFavouriteTvShows(string userId);
    }
}
