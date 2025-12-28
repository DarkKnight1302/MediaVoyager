namespace MediaVoyager.Clients
{
    public interface IOmdbClient
    {
        Task<string> TryGetImdbRatingAsync(string imdbId);
    }
}
