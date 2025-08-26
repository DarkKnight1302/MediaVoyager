using MediaVoyager.Constants;
using MediaVoyager.Models;
using MediaVoyager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewHorizonLib.Services.Interfaces;

namespace MediaVoyager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RecommendationController : ControllerBase
    {
        private readonly IMediaRecommendationService mediaRecommendationService;
        private readonly ILogger<RecommendationController> logger;
        private readonly ITokenService tokenService;

        public RecommendationController(
            IMediaRecommendationService mediaRecommendationService,
            ILogger<RecommendationController> logger,
            ITokenService tokenService)
        {
            this.mediaRecommendationService = mediaRecommendationService;
            this.tokenService = tokenService;
            this.logger = logger;
        }

        [HttpGet("favourite/movie")]
        [Authorize]
        public async Task<IActionResult> GetMovieRecommendation(string userId)
        {
            bool isValidAuth = this.tokenService.IsValidAuth(userId, HttpContext, GlobalConstant.Issuer);
            
            if (!isValidAuth)
            {
                return Unauthorized();
            }
            MovieResponse movieResponse = await this.mediaRecommendationService.GetMovieRecommendationForUser(userId);
            if (movieResponse == null)
            {
                return NotFound();
            }
            return Ok(movieResponse);
        }
    }
}
