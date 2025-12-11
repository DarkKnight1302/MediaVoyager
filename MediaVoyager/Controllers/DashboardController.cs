using MediaVoyager.Models.Dashboard;
using MediaVoyager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediaVoyager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService dashboardService;
        private readonly ILogger<DashboardController> logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
    this.dashboardService = dashboardService;
       this.logger = logger;
  }

   /// <summary>
        /// Get all dashboard metrics
      /// </summary>
        /// <param name="days">Number of days to look back (default: 30)</param>
        [HttpGet]
      public async Task<ActionResult<DashboardMetrics>> GetDashboardMetrics([FromQuery] int days = 30)
        {
            try
         {
          var metrics = await dashboardService.GetDashboardMetricsAsync(days);
 return Ok(metrics);
       }
         catch (Exception ex)
            {
    logger.LogError(ex, "Error getting dashboard metrics");
    return StatusCode(500, "Error retrieving dashboard metrics");
            }
      }

        /// <summary>
        /// Get user signup metrics with date-wise breakdown
      /// </summary>
        [HttpGet("signups")]
  public async Task<ActionResult<UserSignupMetrics>> GetUserSignupMetrics([FromQuery] int days = 30)
        {
       try
        {
     var metrics = await dashboardService.GetUserSignupMetricsAsync(days);
          return Ok(metrics);
            }
            catch (Exception ex)
            {
  logger.LogError(ex, "Error getting user signup metrics");
       return StatusCode(500, "Error retrieving user signup metrics");
            }
        }

   /// <summary>
      /// Get daily and monthly active users
        /// </summary>
    [HttpGet("active-users")]
     public async Task<ActionResult<ActiveUsersMetrics>> GetActiveUsersMetrics([FromQuery] int days = 30)
        {
   try
     {
         var metrics = await dashboardService.GetActiveUsersMetricsAsync(days);
           return Ok(metrics);
            }
         catch (Exception ex)
       {
     logger.LogError(ex, "Error getting active users metrics");
      return StatusCode(500, "Error retrieving active users metrics");
            }
        }

        /// <summary>
        /// Get movie and TV show recommendation metrics
        /// </summary>
        [HttpGet("recommendations")]
 public async Task<ActionResult<RecommendationMetrics>> GetRecommendationMetrics([FromQuery] int days = 30)
        {
         try
     {
                var metrics = await dashboardService.GetRecommendationMetricsAsync(days);
      return Ok(metrics);
         }
            catch (Exception ex)
    {
       logger.LogError(ex, "Error getting recommendation metrics");
   return StatusCode(500, "Error retrieving recommendation metrics");
       }
        }

        /// <summary>
        /// Get search metrics including users who searched for movies/TV shows
        /// </summary>
        [HttpGet("searches")]
        public async Task<ActionResult<SearchMetrics>> GetSearchMetrics([FromQuery] int days = 30)
        {
        try
    {
     var metrics = await dashboardService.GetSearchMetricsAsync(days);
       return Ok(metrics);
      }
            catch (Exception ex)
      {
     logger.LogError(ex, "Error getting search metrics");
return StatusCode(500, "Error retrieving search metrics");
 }
    }

      /// <summary>
/// Get watchlist metrics
        /// </summary>
        [HttpGet("watchlist")]
        public async Task<ActionResult<WatchlistMetrics>> GetWatchlistMetrics([FromQuery] int days = 30)
        {
            try
     {
        var metrics = await dashboardService.GetWatchlistMetricsAsync(days);
     return Ok(metrics);
 }
    catch (Exception ex)
 {
           logger.LogError(ex, "Error getting watchlist metrics");
           return StatusCode(500, "Error retrieving watchlist metrics");
 }
   }
    }
}
