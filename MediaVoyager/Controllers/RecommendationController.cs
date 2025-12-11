using MediaVoyager.Constants;
using MediaVoyager.Entities;
using MediaVoyager.Models;
using MediaVoyager.Repositories;
using MediaVoyager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewHorizonLib.Attributes;
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
        private readonly IUserActivityRepository userActivityRepository;

        public RecommendationController(
            IMediaRecommendationService mediaRecommendationService,
            ILogger<RecommendationController> logger,
            ITokenService tokenService,
            IUserActivityRepository userActivityRepository)
        {
            this.mediaRecommendationService = mediaRecommendationService;
            this.tokenService = tokenService;
            this.logger = logger;
            this.userActivityRepository = userActivityRepository;
        }

        [HttpGet("movie")]
        [Authorize]
        [RateLimit(20, 720)]
        public async Task<IActionResult> GetMovieRecommendation()
        {
            string userId = HttpContext.Request.Headers["x-uid"].FirstOrDefault();
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
            
            // Log movie recommendation activity
            await userActivityRepository.LogActivityAsync(userId, ActivityTypes.MovieRecommendation, movieResponse.Title);
                
            return Ok(movieResponse);
        }

        [HttpGet("tvshow")]
        [Authorize]
        [RateLimit(20, 720)]
        public async Task<IActionResult> GetTvShowRecommendation()
        {
            string userId = HttpContext.Request.Headers["x-uid"].FirstOrDefault();
            bool isValidAuth = this.tokenService.IsValidAuth(userId, HttpContext, GlobalConstant.Issuer);

            if (!isValidAuth)
            {
                return Unauthorized();
            }
            TvShowResponse tvShowResponse = await this.mediaRecommendationService.GetTvShowRecommendationForUser(userId);
            if (tvShowResponse == null)
            {
                return NotFound();
            }
     
            // Log TV show recommendation activity
            await userActivityRepository.LogActivityAsync(userId, ActivityTypes.TvRecommendation, tvShowResponse.Title);
                
            return Ok(tvShowResponse);
        }
    }
}
