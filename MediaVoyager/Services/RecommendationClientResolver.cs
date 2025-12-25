using MediaVoyager.Clients;
using MediaVoyager.Models;
using MediaVoyager.Services.Interfaces;

namespace MediaVoyager.Services
{
    public sealed class RecommendationClientResolver : IRecommendationClientResolver
    {
        private readonly IRecommendationClient gemini;
        private readonly IRecommendationClient groq;

        public RecommendationClientResolver(IGeminiRecommendationClient gemini, IGroqRecommendationClient groq)
        {
            this.gemini = (IRecommendationClient)gemini;
            this.groq = (IRecommendationClient)groq;
        }

        public IRecommendationClient Resolve(RecommendationProvider provider)
        {
            return provider switch
            {
                RecommendationProvider.Groq => this.groq,
                _ => this.gemini
            };
        }
    }
}
