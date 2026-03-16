namespace MediaVoyager.Models.Dashboard
{
    public class FavouritesMetrics
    {
        public int TotalMovieFavourites { get; set; }
        public int TotalTvFavourites { get; set; }
        public int UniqueUsersWithFavourites { get; set; }
        public List<DateCount> MovieFavouritesByDate { get; set; } = new List<DateCount>();
        public List<DateCount> TvFavouritesByDate { get; set; } = new List<DateCount>();
    }
}
