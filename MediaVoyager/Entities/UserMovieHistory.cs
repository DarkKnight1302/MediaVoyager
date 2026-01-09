using MediaVoyager.Models;

namespace MediaVoyager.Entities
{
    public class UserMovieHistory
    {
        public string id { get; set; }

        public HashSet<Movie> movies { get; set; } = new HashSet<Movie>();
    }
}
