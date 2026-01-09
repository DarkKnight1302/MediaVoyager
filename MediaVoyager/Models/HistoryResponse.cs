namespace MediaVoyager.Models
{
    public class HistoryResponse
    {
        public List<Movie> movies { get; set; } = new List<Movie>();

        public List<TvShow> tvShows { get; set; } = new List<TvShow>();
    }
}
