using MediaVoyager.Models;

namespace MediaVoyager.Entities
{
    public class UserTvHistory
    {
        public string id { get; set; }

        public HashSet<TvShow> tvShows { get; set; } = new HashSet<TvShow>();
    }
}
