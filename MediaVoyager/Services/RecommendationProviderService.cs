using MediaVoyager.Models;
using MediaVoyager.Services.Interfaces;

namespace MediaVoyager.Services
{
    /// <summary>
    /// Singleton service that stores the current recommendation provider in-memory.
    /// </summary>
    public sealed class RecommendationProviderService : IRecommendationProviderService
    {
        private RecommendationProvider _currentProvider = RecommendationProvider.Groq;
        private readonly object _lock = new();

        public RecommendationProvider CurrentProvider
        {
            get
            {
                lock (_lock)
                {
                    return _currentProvider;
                }
            }
        }

        public void SetProvider(RecommendationProvider provider)
        {
            lock (_lock)
            {
                _currentProvider = provider;
                Console.WriteLine($"[RecommendationProviderService] Provider switched to: {provider}");
            }
        }
    }
}
