using MediaVoyager.Models;

namespace MediaVoyager.Services.Interfaces
{
    /// <summary>
    /// Service to manage the current recommendation provider selection.
    /// Stores the provider in-memory as a singleton.
    /// </summary>
    public interface IRecommendationProviderService
    {
        /// <summary>
        /// Gets the currently selected recommendation provider.
        /// </summary>
        RecommendationProvider CurrentProvider { get; }

        /// <summary>
        /// Sets the recommendation provider.
        /// </summary>
        /// <param name="provider">The provider to use.</param>
        void SetProvider(RecommendationProvider provider);
    }
}
