using NewHorizonLib.Services;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MediaVoyager.Clients
{
    public sealed class OmdbClient : IOmdbClient
    {
        private readonly string _omdbApiKey;

        public OmdbClient(ISecretService secretService)
        {
            _omdbApiKey = secretService.GetSecretValue("omdb_api_key");
        }

        public async Task<string> TryGetImdbRatingAsync(string imdbId)
        {
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                Console.WriteLine("[Omdb] IMDb id not provided");
                return null;
            }

            if (string.IsNullOrWhiteSpace(_omdbApiKey))
            {
                Console.WriteLine("[Omdb] OMDb api key not configured");
                return null;
            }

            try
            {
                using HttpClient httpClient = new HttpClient();
                string url = $"http://www.omdbapi.com/?i={Uri.EscapeDataString(imdbId)}&apikey={Uri.EscapeDataString(_omdbApiKey)}";
                Console.WriteLine($"[Omdb] Fetching IMDb rating imdbId={imdbId}");

                OmdbMovieResponse omdbResponse = await httpClient.GetFromJsonAsync<OmdbMovieResponse>(url).ConfigureAwait(false);
                if (omdbResponse == null)
                {
                    Console.WriteLine("[Omdb] OMDb returned null response");
                    return null;
                }

                if (!string.Equals(omdbResponse.Response, "True", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[Omdb] OMDb response not successful: Response={omdbResponse.Response} Error={omdbResponse.Error}");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(omdbResponse.ImdbRating) || string.Equals(omdbResponse.ImdbRating, "N/A", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[Omdb] OMDb imdbRating not available");
                    return null;
                }

                Console.WriteLine($"[Omdb] OMDb imdbRating={omdbResponse.ImdbRating}");
                return omdbResponse.ImdbRating;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Omdb] OMDb lookup failed: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private sealed class OmdbMovieResponse
        {
            [JsonPropertyName("Response")] public string Response { get; set; }
            [JsonPropertyName("Error")] public string Error { get; set; }
            [JsonPropertyName("imdbRating")] public string ImdbRating { get; set; }
        }
    }
}
