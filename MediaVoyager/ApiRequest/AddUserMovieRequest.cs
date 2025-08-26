using TMDbLib.Objects.Search;

namespace MediaVoyager.ApiRequest
{
    public class AddUserMovieRequest
    {
        public string userId { get; set; }

        public List<SearchMovie> favouriteMovies { get; set; }

    }
}
