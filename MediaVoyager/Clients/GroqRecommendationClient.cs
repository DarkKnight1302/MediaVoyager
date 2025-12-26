namespace MediaVoyager.Clients
{
    using NewHorizonLib.Services;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A client for getting movie/TV recommendations from the Groq Chat Completions API.
    /// This client includes the same (shared) rate-limiting mechanism as GeminiRecommendationClient.
    /// </summary>
    public class GroqRecommendationClient : IDisposable, IGroqRecommendationClient, IRecommendationClient
    {
        private readonly HttpClient _httpClient;

        // --- Rate Limiting State ---
        private static readonly int MaxRequests = 9;
        private static readonly TimeSpan TimePeriod = TimeSpan.FromMinutes(1);
        private static readonly Queue<DateTime> RequestTimestamps = new Queue<DateTime>();
        private static readonly SemaphoreSlim RateLimitSemaphore = new SemaphoreSlim(1, 1);

        // --- Daily Rate Limiting State (combined across all methods) ---
        private static readonly int MaxRequestsPerDay = 1000;
        private static DateTime CurrentDay = DateTime.UtcNow.Date;
        private static int DailyRequestCount = 0;

        private const string GroqChatCompletionsUrl = "https://api.groq.com/openai/v1/chat/completions";
        private const string DefaultModel = "openai/gpt-oss-120b";

        // --- Retry ---
        private const int MaxRetryAttempts = 3;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan TooManyRequestsRetryDelay = TimeSpan.FromSeconds(4);

        public GroqRecommendationClient(ISecretService secretService, ILogger<GroqRecommendationClient> logger)
        {
            // logger parameter is kept to preserve constructor signature used by DI,
            // but this client now logs via Console.WriteLine.

            string groqApiKey = secretService.GetSecretValue("groq_api_key");
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", groqApiKey);
        }

        public async Task<string> GetMovieRecommendationAsync(List<string> favoriteMovies, List<string> watchHistory, int temperature = 1)
        {
            return await ExecuteWithRetryAsync(
                operationName: "[Groq][Movie]",
                operation: async () =>
                {
                    await WaitForRateLimitSlotAsync();

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Groq][Movie] Sending request for favorites: {string.Join(", ", favoriteMovies.Take(2))}...");

                    var requestBody = BuildGroqRequestForMovies(favoriteMovies, watchHistory, temperature);
                    var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });

                    using var request = new HttpRequestMessage(HttpMethod.Post, GroqChatCompletionsUrl)
                    {
                        Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                    };

                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var groqResponse = JsonSerializer.Deserialize<ChatCompletionsResponse>(responseBody);

                    return groqResponse?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
                });
        }

        public async Task<string> GetTvShowRecommendationAsync(List<string> favouriteTvShows, List<string> watchHistory, int temperature)
        {
            return await ExecuteWithRetryAsync(
                operationName: "[Groq][TV]",
                operation: async () =>
                {
                    await WaitForRateLimitSlotAsync();

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Groq][TV] Sending request for favorites: {string.Join(", ", favouriteTvShows.Take(2))}...");

                    var requestBody = BuildGroqRequestForTvShows(favouriteTvShows, watchHistory, temperature);
                    var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });

                    using var request = new HttpRequestMessage(HttpMethod.Post, GroqChatCompletionsUrl)
                    {
                        Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                    };

                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var groqResponse = JsonSerializer.Deserialize<ChatCompletionsResponse>(responseBody);

                    return groqResponse?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
                });
        }

        private async Task<string> ExecuteWithRetryAsync(string operationName, Func<Task<string>> operation)
        {
            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {operationName} Rate limited (HTTP 429) (attempt {attempt}/{MaxRetryAttempts}): {ex.Message}");

                    if (attempt == MaxRetryAttempts)
                    {
                        // Bubble up after final attempt so callers can differentiate 429 from other failures.
                        throw;
                    }

                    await Task.Delay(TooManyRequestsRetryDelay);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {operationName} Error parsing Groq API response (attempt {attempt}/{MaxRetryAttempts}): {ex.Message}");

                    if (attempt == MaxRetryAttempts)
                    {
                        return string.Empty;
                    }

                    await Task.Delay(RetryDelay);
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {operationName} Error calling Groq API (attempt {attempt}/{MaxRetryAttempts}): {ex.Message}");

                    if (attempt == MaxRetryAttempts)
                    {
                        return string.Empty;
                    }

                    await Task.Delay(RetryDelay);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {operationName} Exception in recommendation (attempt {attempt}/{MaxRetryAttempts}): {e.Message}\n{e.StackTrace}");

                    if (attempt == MaxRetryAttempts)
                    {
                        return string.Empty;
                    }

                    await Task.Delay(RetryDelay);
                }
            }

            return string.Empty;
        }

        private async Task WaitForRateLimitSlotAsync()
        {
            await RateLimitSemaphore.WaitAsync();
            try
            {
                if (DateTime.UtcNow.Date != CurrentDay)
                {
                    CurrentDay = DateTime.UtcNow.Date;
                    DailyRequestCount = 0;
                }

                if (DailyRequestCount >= MaxRequestsPerDay)
                {
                    throw new HttpRequestException(
                        $"Daily Groq API request limit exceeded ({MaxRequestsPerDay} per day).",
                        inner: null,
                        statusCode: HttpStatusCode.TooManyRequests);
                }

                while (RequestTimestamps.Any() && (DateTime.UtcNow - RequestTimestamps.Peek()) > TimePeriod)
                {
                    RequestTimestamps.Dequeue();
                }

                if (RequestTimestamps.Count >= MaxRequests)
                {
                    var oldestRequestTime = RequestTimestamps.Peek();
                    var timePassedSinceOldest = DateTime.UtcNow - oldestRequestTime;
                    var timeToWait = TimePeriod - timePassedSinceOldest;

                    if (timeToWait > TimeSpan.Zero)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Groq] Rate limit hit. Waiting for {timeToWait.TotalSeconds:F1} seconds...");
                        await Task.Delay(timeToWait);
                    }

                    RequestTimestamps.Dequeue();
                }

                if (DateTime.UtcNow.Date != CurrentDay)
                {
                    CurrentDay = DateTime.UtcNow.Date;
                    DailyRequestCount = 0;
                }

                RequestTimestamps.Enqueue(DateTime.UtcNow);
                DailyRequestCount++;
            }
            finally
            {
                RateLimitSemaphore.Release();
            }
        }

        private ChatCompletionsRequest BuildGroqRequestForMovies(List<string> favoriteMovies, List<string> watchHistory, int temperature)
        {
            Func<List<string>, string> formatList = items => $"[`{string.Join("`, `", items.Select(m => m.Replace("`", "\\`")))}`]";

            string prompt1 = $"Favourite Movies : {formatList(favoriteMovies)}";
            string prompt2 = $"Watch history : {formatList(watchHistory)}";

            return new ChatCompletionsRequest
            {
                Model = DefaultModel,
                Temperature = temperature,
                MaxCompletionTokens = 8192,
                TopP = 1,
                Stream = false,
                Stop = null,
                ReasoningEffort = "high",
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Role = "system",
                        Content = "You're a taste based movie recommendation bot, you have to understand the user's taste in movies based on list of provided favourite movies only (not watch history) and you will recommend good movie based on given favorites only and the recommended movie should not be part of watch history. Your output should only be the `name of the movie` and `release year` in next line and only recommend one movie. Example response : <Movie name>\n<Release year>"
                    },
                    new ChatMessage { Role = "user", Content = $"{prompt1}\n{prompt2}" }
                }
            };
        }

        private ChatCompletionsRequest BuildGroqRequestForTvShows(List<string> favouriteTvShows, List<string> watchHistory, int temperature)
        {
            Func<List<string>, string> formatList = items => $"[`{string.Join("`, `", items.Select(m => m.Replace("`", "\\`")))}`]";

            string prompt1 = $"Favourite TV Shows : {formatList(favouriteTvShows)}";
            string prompt2 = $"Watch history : {formatList(watchHistory)}";

            return new ChatCompletionsRequest
            {
                Model = DefaultModel,
                Temperature = temperature,
                MaxCompletionTokens = 8192,
                TopP = 1,
                Stream = false,
                Stop = null,
                ReasoningEffort = "high",
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Role = "system",
                        Content = "You're a taste based tv show recommendation bot, you have to understand the user's taste in tv shows based on list of provided favourite tv shows only (not watch history) and you will recommend a tv show based on given favourites only and the recommended tv show should not be part of watch history, Your output should only be the `name of the show` and `first air year` in next line and only recommend one show. Example response : <Tv show name>\n<First air year>"
                    },
                    new ChatMessage { Role = "user", Content = $"{prompt1}\n{prompt2}" }
                }
            };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            RateLimitSemaphore?.Dispose();
            GC.SuppressFinalize(this);
        }

        #region DTOs

        private class ChatCompletionsRequest
        {
            [JsonPropertyName("messages")] public List<ChatMessage> Messages { get; set; }
            [JsonPropertyName("model")] public string Model { get; set; }

            [JsonPropertyName("temperature")] public int Temperature { get; set; } = 1;

            [JsonPropertyName("max_completion_tokens")] public int MaxCompletionTokens { get; set; } = 8192;

            [JsonPropertyName("top_p")] public int TopP { get; set; } = 1;

            [JsonPropertyName("stream")] public bool Stream { get; set; } = false;

            [JsonPropertyName("include_reasoning")] public bool IncludeReasoning { get; set; } = false;

            [JsonPropertyName("reasoning_effort")] public string ReasoningEffort { get; set; } = "high";

            // Groq supports null stop; model it explicitly.
            [JsonPropertyName("stop")] public object Stop { get; set; }
        }

        private class ChatMessage
        {
            [JsonPropertyName("role")] public string Role { get; set; }
            [JsonPropertyName("content")] public string Content { get; set; }
        }

        private class ChatCompletionsResponse
        {
            [JsonPropertyName("choices")] public List<Choice> Choices { get; set; }
        }

        private class Choice
        {
            [JsonPropertyName("message")] public ChatMessage Message { get; set; }
        }

        #endregion
    }
}

