namespace MediaVoyager.Clients
{
    using NewHorizonLib.Services;
    // GeminiRecommendationClient.cs

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A client for getting movie recommendations from the Google Gemini API.
    /// This client includes a rate-limiting mechanism to ensure that no more than
    /// 2 requests are sent per minute.
    /// </summary>
    public class GeminiRecommendationClient : IDisposable, IGeminiRecommendationClient
    {
        private readonly HttpClient _httpClient;

        private readonly ILogger logger;
        // --- Rate Limiting State ---
        // The maximum number of requests allowed in the defined time period.
        private static readonly int MaxRequests = 2;
        // The time period over which the request limit is enforced.
        private static readonly TimeSpan TimePeriod = TimeSpan.FromMinutes(1);
        // A queue to store the timestamps of recent requests.
        private static readonly Queue<DateTime> RequestTimestamps = new Queue<DateTime>();
        // A semaphore to ensure thread-safe access to the request timestamps queue.
        private static readonly SemaphoreSlim RateLimitSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the GeminiRecommendationClient.
        /// </summary>
        /// <param name="apiKey">Your Google Gemini API key.</param>
        public GeminiRecommendationClient(ISecretService secretService, ILogger<GeminiRecommendationClient> logger)
        {
            this.logger = logger;
            string geminiApiKey = secretService.GetSecretValue("gemini_api_key");
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // Correctly add the API key to the request header as per the cURL command.
            _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", geminiApiKey);
        }

        /// <summary>
        /// Gets a movie recommendation from the Gemini API based on user preferences.
        /// This method is rate-limited to 2 requests per minute.
        /// </summary>
        /// <param name="favoriteMovies">A list of the user's favorite movies.</param>
        /// <param name="watchHistory">A list of movies the user has already watched.</param>
        /// <returns>The name of the recommended movie as a string.</returns>
        public async Task<string> GetMovieRecommendationAsync(List<string> favoriteMovies, List<string> watchHistory, int temperature = 1)
        {
            // 1. Wait for a slot in the rate limit window before proceeding.
            await WaitForRateLimitSlotAsync();

            // 2. Prepare the request for the Gemini API
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Sending request for favorites: {string.Join(", ", favoriteMovies.Take(2))}...");

            // Correctly use the gemini-2.5-pro model and remove the API key from the URL.
            var requestUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

            var requestBody = BuildGeminiRequest(favoriteMovies, watchHistory, temperature);
            var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            // 3. Send the request and process the response
            try
            {
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);

                // Extract the movie name from the first candidate in the response.
                return geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim()
                       ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                // Handle potential network or API errors
                this.logger.LogError($"Error calling Gemini API: {ex.Message}");
            }
            catch (JsonException ex)
            {
                // Handle errors in parsing the response
                this.logger.LogError($"Error parsing API response: {ex.Message}");
            }
            catch (Exception e)
            {
                this.logger.LogError($"Exception in movie recommendation : {e.Message} \n {e.StackTrace}", e);
            }
            return string.Empty;
        }

        /// <summary>
        /// Enforces the rate limit by delaying execution if too many requests have been made recently.
        /// This method is thread-safe.
        /// </summary>
        private async Task WaitForRateLimitSlotAsync()
        {
            // Acquire the semaphore to ensure only one thread modifies the queue at a time.
            await RateLimitSemaphore.WaitAsync();
            try
            {
                // First, remove any timestamps that are now outside the 1-minute window.
                while (RequestTimestamps.Any() && (DateTime.UtcNow - RequestTimestamps.Peek()) > TimePeriod)
                {
                    RequestTimestamps.Dequeue();
                }

                // If we have already made the maximum number of requests in the window, we must wait.
                if (RequestTimestamps.Count >= MaxRequests)
                {
                    var oldestRequestTime = RequestTimestamps.Peek();
                    var timePassedSinceOldest = DateTime.UtcNow - oldestRequestTime;
                    var timeToWait = TimePeriod - timePassedSinceOldest;

                    if (timeToWait > TimeSpan.Zero)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Rate limit hit. Waiting for {timeToWait.TotalSeconds:F1} seconds...");
                        await Task.Delay(timeToWait);
                    }

                    // After waiting, the oldest request is now outside the window, so we remove it.
                    RequestTimestamps.Dequeue();
                }

                // Add the timestamp for the current request.
                RequestTimestamps.Enqueue(DateTime.UtcNow);
            }
            finally
            {
                // Release the semaphore.
                RateLimitSemaphore.Release();
            }
        }

        /// <summary>
        /// Builds the request body for the Gemini API call.
        /// </summary>
        private GeminiRequest BuildGeminiRequest(List<string> favoriteMovies, List<string> watchHistory, int temperature)
        {
            // Helper function to format a list of movies into a string like "['Movie1', 'Movie2']"
            Func<List<string>, string> formatMovieList = movies => $"[`{string.Join("`, `", movies.Select(m => m.Replace("`", "\\`")))}`]";

            string prompt1 = $"Favourite Movies : {formatMovieList(favoriteMovies)}";
            string prompt2 = $"Watch history : {formatMovieList(watchHistory)}";

            var request = new GeminiRequest
            {
                SystemInstruction = new SystemInstruction
                {
                    Parts = new List<Part>
                {
                    new Part { Text = "You're a movie recommendation bot, you will recommend good movie based on given favorites as reference and the recommended movie should not be part of watch history. Your output should only be the `name of the movie` and `release year` in next line and only recommend one movie. Example response : <Movie name>\n<Release year>" }
                }
                },
                Contents = new List<Content>
            {
                new Content
                {
                    Parts = new List<Part> { new Part { Text = prompt1 }, new Part { Text = prompt2 } }
                }
            }
            };
            request.generationConfig.temperature = temperature;
            return request;
        }

        /// <summary>
        /// Builds the request body for the Gemini API call.
        /// </summary>
        private GeminiRequest BuildGeminiRequestForTvShows(List<string> favouriteTvShows, List<string> watchHistory, int temperature)
        {
            // Helper function to format a list of movies into a string like "['Movie1', 'Movie2']"
            Func<List<string>, string> formatMovieList = movies => $"[`{string.Join("`, `", movies.Select(m => m.Replace("`", "\\`")))}`]";

            string prompt1 = $"Favourite Tv shows : {formatMovieList(favouriteTvShows)}";
            string prompt2 = $"Watch history : {formatMovieList(watchHistory)}";

            var request = new GeminiRequest
            {
                SystemInstruction = new SystemInstruction
                {
                    Parts = new List<Part>
                {
                    new Part { Text = "You're a tv show recommendation bot, you will recommend a tv show based on given favourites as reference and the recommended tv show should not be part of watch history, Your output should only be the `name of the show` and `first air year` in next line and only recommend one show. Example response : <Tv show name>\n<First air year>" }
                }
                },
                Contents = new List<Content>
            {
                new Content
                {
                    Parts = new List<Part> { new Part { Text = prompt1 }, new Part { Text = prompt2 } }
                }
            }
            };
            request.generationConfig.temperature = temperature;
            return request;
        }

        /// <summary>
        /// Disposes of the HttpClient and SemaphoreSlim.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
            RateLimitSemaphore?.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<string> GetTvShowRecommendationAsync(List<string> favouriteTvShows, List<string> watchHistory, int temperature)
        {
            // 1. Wait for a slot in the rate limit window before proceeding.
            await WaitForRateLimitSlotAsync();

            // 2. Prepare the request for the Gemini API
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Sending request for favorites: {string.Join(", ", favouriteTvShows.Take(2))}...");

            // Correctly use the gemini-2.5-pro model and remove the API key from the URL.
            var requestUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

            var requestBody = BuildGeminiRequestForTvShows(favouriteTvShows, watchHistory, temperature);
            var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            // 3. Send the request and process the response
            try
            {
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);

                // Extract the movie name from the first candidate in the response.
                return geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim()
                       ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                // Handle potential network or API errors
                this.logger.LogError($"Error calling Gemini API: {ex.Message}");
            }
            catch (JsonException ex)
            {
                // Handle errors in parsing the response
                this.logger.LogError($"Error parsing API response: {ex.Message}");
            }
            catch (Exception e)
            {
                this.logger.LogError($"Exception in tv show recommendation : {e.Message} \n {e.StackTrace}", e);
            }
            return string.Empty;
        }

        // --- C# Models for JSON Serialization/Deserialization ---
        #region DTOs

        private class GeminiRequest
        {
            [JsonPropertyName("system_instruction")]
            public SystemInstruction SystemInstruction { get; set; }

            [JsonPropertyName("contents")]
            public List<Content> Contents { get; set; }

            [JsonPropertyName("generationConfig")]
            public GenerationConfig generationConfig = new GenerationConfig();
        }

        private class GenerationConfig
        {
            public int temperature { get; set; } = 1;
        }

        private class SystemInstruction
        {
            [JsonPropertyName("parts")]
            public List<Part> Parts { get; set; }
        }
        private class Content
        {
            [JsonPropertyName("parts")]
            public List<Part> Parts { get; set; }
        }
        private class Part
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        // Response Models (Updated to match the full JSON structure)
        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<Candidate> Candidates { get; set; }

            [JsonPropertyName("usageMetadata")]
            public UsageMetadata UsageMetadata { get; set; }

            [JsonPropertyName("modelVersion")]
            public string ModelVersion { get; set; }

            [JsonPropertyName("responseId")]
            public string ResponseId { get; set; }
        }

        private class Candidate
        {
            [JsonPropertyName("content")]
            public ContentResponse Content { get; set; }

            [JsonPropertyName("finishReason")]
            public string FinishReason { get; set; }

            [JsonPropertyName("index")]
            public int Index { get; set; }
        }

        private class ContentResponse
        {
            [JsonPropertyName("parts")]
            public List<PartResponse> Parts { get; set; }

            [JsonPropertyName("role")]
            public string Role { get; set; }
        }

        private class PartResponse
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        private class UsageMetadata
        {
            [JsonPropertyName("promptTokenCount")]
            public int PromptTokenCount { get; set; }

            [JsonPropertyName("candidatesTokenCount")]
            public int CandidatesTokenCount { get; set; }

            [JsonPropertyName("totalTokenCount")]
            public int TotalTokenCount { get; set; }

            [JsonPropertyName("promptTokensDetails")]
            public List<PromptTokensDetail> PromptTokensDetails { get; set; }

            [JsonPropertyName("thoughtsTokenCount")]
            public int ThoughtsTokenCount { get; set; }
        }

        private class PromptTokensDetail
        {
            [JsonPropertyName("modality")]
            public string Modality { get; set; }

            [JsonPropertyName("tokenCount")]
            public int TokenCount { get; set; }
        }
        #endregion

    }
}
