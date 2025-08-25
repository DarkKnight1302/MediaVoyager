using MediaVoyager.Models;

namespace MediaVoyager.Entities
{
    public class UserTv
    {
        public string id { get; set; }

        public List<TvShow> favouriteTv { get; set; } = new List<TvShow>();

        public List<TvShow> watchHistory { get; set; } = new List<TvShow>();
    }
}
