using MediaVoyager.Models;
using MediaVoyager.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MediaVoyager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProviderController : ControllerBase
    {
        private readonly IRecommendationProviderService _providerService;

        public ProviderController(IRecommendationProviderService providerService)
        {
            _providerService = providerService;
        }

        /// <summary>
        /// Gets the currently active recommendation provider.
        /// </summary>
        [HttpGet]
        public IActionResult GetCurrentProvider()
        {
            var provider = _providerService.CurrentProvider;
            return Ok(new { provider = provider.ToString() });
        }

        /// <summary>
        /// Sets the recommendation provider.
        /// </summary>
        /// <param name="provider">The provider name: "Gemini" or "Groq"</param>
        [HttpPost("{provider}")]
        public IActionResult SetProvider(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return BadRequest(new { error = "Provider name is required" });
            }

            if (!Enum.TryParse<RecommendationProvider>(provider, ignoreCase: true, out var parsedProvider))
            {
                return BadRequest(new { error = $"Invalid provider '{provider}'. Valid values are: Gemini, Groq" });
            }

            _providerService.SetProvider(parsedProvider);
            return Ok(new { provider = parsedProvider.ToString(), message = $"Provider switched to {parsedProvider}" });
        }

        /// <summary>
        /// Switches to Gemini provider.
        /// </summary>
        [HttpPost("gemini")]
        public IActionResult UseGemini()
        {
            _providerService.SetProvider(RecommendationProvider.Gemini);
            return Ok(new { provider = "Gemini", message = "Provider switched to Gemini" });
        }

        /// <summary>
        /// Switches to Groq provider.
        /// </summary>
        [HttpPost("groq")]
        public IActionResult UseGroq()
        {
            _providerService.SetProvider(RecommendationProvider.Groq);
            return Ok(new { provider = "Groq", message = "Provider switched to Groq" });
        }
    }
}
