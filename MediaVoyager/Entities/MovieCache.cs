using TMDbLib.Objects.Movies;

namespace MediaVoyager.Entities
{
    // Cosmos DB document for caching TMDb Movie
    public class MovieCache
    {
        // Cosmos DB document id - use TMDb Movie.Id as string
        public string id { get; set; }

        // Cached TMDb movie payload
        public Movie data { get; set; }
    }
}
