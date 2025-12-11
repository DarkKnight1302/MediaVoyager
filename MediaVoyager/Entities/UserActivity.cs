namespace MediaVoyager.Entities
{
    public class UserActivity
    {
        public string id { get; set; }
        
        public string UserId { get; set; }
        
        public DateTimeOffset ActivityDate { get; set; }
   
        public string ActivityType { get; set; } // Login, Search, Watchlist, Recommendation, etc.
        
        public string Details { get; set; } // Additional details about the activity
        
        public DateTimeOffset CreatedAt { get; set; }
    }

    public static class ActivityTypes
    {
        public const string Login = "Login";
      public const string MovieSearch = "MovieSearch";
        public const string TvSearch = "TvSearch";
        public const string MovieRecommendation = "MovieRecommendation";
    public const string TvRecommendation = "TvRecommendation";
        public const string AddMovieToWatchlist = "AddMovieToWatchlist";
  public const string AddTvToWatchlist = "AddTvToWatchlist";
        public const string AddMovieToFavourites = "AddMovieToFavourites";
        public const string AddTvToFavourites = "AddTvToFavourites";
  }
}
