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
        private readonly IErrorNotificationService errorNotificationService;

        public RecommendationController(
            IMediaRecommendationService mediaRecommendationService,
            ILogger<RecommendationController> logger,
            ITokenService tokenService,
            IUserActivityRepository userActivityRepository,
            IErrorNotificationService errorNotificationService)
        {
            this.mediaRecommendationService = mediaRecommendationService;
            this.tokenService = tokenService;
            this.logger = logger;
            this.userActivityRepository = userActivityRepository;
            this.errorNotificationService = errorNotificationService;
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

            try
            {
                MovieResponse movieResponse = await this.mediaRecommendationService.GetMovieRecommendationForUser(userId);
                if (movieResponse == null)
                {
                    await this.errorNotificationService.SendErrorNotificationAsync(
                        "GET /Recommendation/movie",
                        userId,
                        "Empty Response",
                        "Movie recommendation service returned null response.");
                    return NotFound();
                }

                // Log movie recommendation activity
                await userActivityRepository.LogActivityAsync(userId, ActivityTypes.MovieRecommendation, movieResponse.Title);

                return Ok(movieResponse);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error getting movie recommendation for user {UserId}", userId);
                await this.errorNotificationService.SendErrorNotificationAsync(
                    "GET /Recommendation/movie",
                    userId,
                    "Exception",
                    $"Exception: {ex.Message}");
                return StatusCode(500, "An error occurred while getting movie recommendation.");
            }
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

            try
            {
                TvShowResponse tvShowResponse = await this.mediaRecommendationService.GetTvShowRecommendationForUser(userId);
                if (tvShowResponse == null)
                {
                    await this.errorNotificationService.SendErrorNotificationAsync(
                        "GET /Recommendation/tvshow",
                        userId,
                        "Empty Response",
                        "TV show recommendation service returned null response.");
                    return NotFound();
                }

                // Log TV show recommendation activity
                await userActivityRepository.LogActivityAsync(userId, ActivityTypes.TvRecommendation, tvShowResponse.Title);

                return Ok(tvShowResponse);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error getting TV show recommendation for user {UserId}", userId);
                await this.errorNotificationService.SendErrorNotificationAsync(
                    "GET /Recommendation/tvshow",
                    userId,
                    "Exception",
                    $"Exception: {ex.Message}");
                return StatusCode(500, "An error occurred while getting TV show recommendation.");
            }
        }
    }
}
