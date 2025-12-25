using MediaVoyager.Clients;
using MediaVoyager.Models;

namespace MediaVoyager.Services.Interfaces
{
    public interface IRecommendationClientResolver
    {
        IRecommendationClient Resolve(RecommendationProvider provider);
    }
}

