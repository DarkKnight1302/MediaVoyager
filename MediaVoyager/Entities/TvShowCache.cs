using TMDbLib.Objects.TvShows;

namespace MediaVoyager.Entities
{
    // Cosmos DB document for caching TMDb TvShow
    public class TvShowCache
    {
        // Cosmos DB document id - use TMDb TvShow.Id as string
        public string id { get; set; }

        // Cached TMDb TV show payload
        public TvShow data { get; set; }
    }
}
