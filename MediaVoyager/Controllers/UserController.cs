using MediaVoyager.ApiRequest;
using MediaVoyager.Constants;
using MediaVoyager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewHorizonLib.Attributes;
using NewHorizonLib.Services.Interfaces;

namespace MediaVoyager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ITokenService tokenService;
        private readonly IUserMediaService userMediaService;

        public UserController(ITokenService tokenService, IUserMediaService userMediaService)
        {
            this.tokenService = tokenService;
            this.userMediaService = userMediaService;
        }

        [Authorize]
        [HttpPost("movies/addFavourites")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> AddFavouriteMovies([FromBody] AddUserMovieRequest addUserMovieRequest)
        {
            string userId = HttpContext.Request.Headers["x-uid"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID header is required");
            }

            bool isValid = this.tokenService.IsValidAuth(userId, HttpContext, GlobalConstant.Issuer);

            if (!isValid)
            {
                return Unauthorized();
            }
            await this.userMediaService.AddMoviesToFavourites(userId, addUserMovieRequest.movies);
            return Ok();
        }

        [Authorize]
        [HttpPost("movies/addWatchHistory")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> AddMoviesToWatchHistory([FromBody] AddUserMovieRequest addUserMovieRequest)
        {
            string userId = HttpContext.Request.Headers["x-uid"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID header is required");
            }

            bool isValid = this.tokenService.IsValidAuth(userId, HttpContext, GlobalConstant.Issuer);

            if (!isValid)
            {
                return Unauthorized();
            }
            await this.userMediaService.AddMoviesToWatchHistory(userId, addUserMovieRequest.movies);
            return Ok();
        }

        [Authorize]
        [HttpPost("tv/addFavourites")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> AddFavouriteTvShows([FromBody] AddUserTvRequest addUserTvRequest)
        {
            string userId = HttpContext.Request.Headers["x-uid"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID header is required");
            }

            bool isValid = this.tokenService.IsValidAuth(userId, HttpContext, GlobalConstant.Issuer);

            if (!isValid)
            {
                return Unauthorized();
            }
            await this.userMediaService.AddTvShowsToFavourites(userId, addUserTvRequest.tvShows);
            return Ok();
        }

        [Authorize]
        [HttpPost("tv/addWatchHistory")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> AddTvShowsToWatchHistory([FromBody] AddUserTvRequest addUserTvRequest)
        {
            string userId = HttpContext.Request.Headers["x-uid"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID header is required");
            }

            bool isValid = this.tokenService.IsValidAuth(userId, HttpContext, GlobalConstant.Issuer);

            if (!isValid)
            {
                return Unauthorized();
            }
            await this.userMediaService.AddTvShowsToWatchHistory(userId, addUserTvRequest.tvShows);
            return Ok();
        }
    }
}
