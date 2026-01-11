namespace MediaVoyager.Models.Dashboard
{
 public class DashboardMetrics
    {
        public UserSignupMetrics UserSignups { get; set; }
        public ActiveUsersMetrics ActiveUsers { get; set; }
   public RecommendationMetrics Recommendations { get; set; }
        public SearchMetrics Searches { get; set; }
        public WatchlistMetrics Watchlist { get; set; }
        public ApiFailureMetrics ApiFailures { get; set; } = new();
    }

    public class UserSignupMetrics
    {
   public int TotalUsers { get; set; }
        public List<DateCount> SignupsByDate { get; set; } = new List<DateCount>();
    }

    public class ActiveUsersMetrics
    {
        public int DailyActiveUsers { get; set; }
        public int MonthlyActiveUsers { get; set; }
        public List<DateCount> DailyActiveUsersByDate { get; set; } = new List<DateCount>();
    }

    public class RecommendationMetrics
    {
        public int TotalMovieRecommendations { get; set; }
        public int TotalTvRecommendations { get; set; }
        public List<DateCount> MovieRecommendationsByDate { get; set; } = new List<DateCount>();
        public List<DateCount> TvRecommendationsByDate { get; set; } = new List<DateCount>();
    }

    public class SearchMetrics
    {
        public int TotalMovieSearches { get; set; }
        public int TotalTvSearches { get; set; }
        public int UniqueUsersSearched { get; set; }
     public List<DateCount> MovieSearchesByDate { get; set; } = new List<DateCount>();
        public List<DateCount> TvSearchesByDate { get; set; } = new List<DateCount>();
  public List<DateCount> UniqueUsersByDate { get; set; } = new List<DateCount>();
    }

    public class WatchlistMetrics
    {
        public int TotalMoviesAddedToWatchlist { get; set; }
        public int TotalTvAddedToWatchlist { get; set; }
        public int UniqueUsersWithWatchlist { get; set; }
        public List<DateCount> WatchlistActivityByDate { get; set; } = new List<DateCount>();
    }

    public class DateCount
    {
    public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}
