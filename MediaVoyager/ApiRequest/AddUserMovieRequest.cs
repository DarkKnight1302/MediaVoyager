using TMDbLib.Objects.Search;

namespace MediaVoyager.ApiRequest;

public class AddUserMovieRequest
{
    public List<SearchMovie> movies { get; set; }
}
