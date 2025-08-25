using MediaVoyager.Models;

namespace MediaVoyager.Entities
{
    public class UserMovies
    {
        public string id { get; set; }

        public HashSet<Movie> favouriteMovies { get; set; } = new HashSet<Movie>();

        public HashSet<Movie> watchHistory { get; set; } = new HashSet<Movie>();
    }
}
