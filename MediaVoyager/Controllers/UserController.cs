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

            // Validate that all movies have title and release date
            if (addUserMovieRequest?.movies != null)
            {
                var invalidMovies = addUserMovieRequest.movies
                    .Where(m => string.IsNullOrWhiteSpace(m.Title) || !m.ReleaseDate.HasValue)
                    .ToList();

                if (invalidMovies.Any())
                {
                    return BadRequest("All movies must have a title and release date");
                }
            }

            await this.userMediaService.AddMoviesToFavourites(userId, addUserMovieRequest.movies);
            return Ok();
        }

        [Authorize]
        [HttpPost("movies/removeFromFavourites")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> RemoveMoviesFromFavourites([FromBody] WatchlistMovieRequest request)
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

            if (request?.movieIds == null || !request.movieIds.Any())
            {
                return BadRequest("Movie IDs are required");
            }

            try
            {
                await this.userMediaService.RemoveMoviesFromFavourites(userId, request.movieIds);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("movies/favourites")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> GetFavouriteMovies()
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

            try
            {
                var favourites = await this.userMediaService.GetUserFavouriteMovies(userId);
                return Ok(favourites);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
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

            // Validate that all movies have title and release date
            if (addUserMovieRequest?.movies != null)
            {
                var invalidMovies = addUserMovieRequest.movies
                    .Where(m => string.IsNullOrWhiteSpace(m.Title) || !m.ReleaseDate.HasValue)
                    .ToList();

                if (invalidMovies.Any())
                {
                    return BadRequest("All movies must have a title and release date");
                }
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

            // Validate that all TV shows have name and first air date
            if (addUserTvRequest?.tvShows != null)
            {
                var invalidTvShows = addUserTvRequest.tvShows
                    .Where(tv => string.IsNullOrWhiteSpace(tv.Name) || !tv.FirstAirDate.HasValue)
                    .ToList();

                if (invalidTvShows.Any())
                {
                    return BadRequest("All TV shows must have a name and first air date");
                }
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

            // Validate that all TV shows have name and first air date
            if (addUserTvRequest?.tvShows != null)
            {
                var invalidTvShows = addUserTvRequest.tvShows
                    .Where(tv => string.IsNullOrWhiteSpace(tv.Name) || !tv.FirstAirDate.HasValue)
                    .ToList();

                if (invalidTvShows.Any())
                {
                    return BadRequest("All TV shows must have a name and first air date");
                }
            }

            await this.userMediaService.AddTvShowsToWatchHistory(userId, addUserTvRequest.tvShows);
            return Ok();
        }

        // Watchlist endpoints
        [Authorize]
        [HttpGet("watchlist")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> GetWatchlist()
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

            try
            {
                var watchlist = await this.userMediaService.GetUserWatchlist(userId);
                return Ok(watchlist);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving watchlist: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPost("movies/addToWatchlist")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> AddMoviesToWatchlist([FromBody] WatchlistMovieRequest request)
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

            if (request?.movieIds == null || !request.movieIds.Any())
            {
                return BadRequest("Movie IDs are required");
            }

            try
            {
                await this.userMediaService.AddMoviesToWatchlist(userId, request.movieIds);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error adding movies to watchlist: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPost("movies/removeFromWatchlist")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> RemoveMoviesFromWatchlist([FromBody] WatchlistMovieRequest request)
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

            if (request?.movieIds == null || !request.movieIds.Any())
            {
                return BadRequest("Movie IDs are required");
            }

            try
            {
                await this.userMediaService.RemoveMoviesFromWatchlist(userId, request.movieIds);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error removing movies from watchlist: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPost("tv/addToWatchlist")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> AddTvShowsToWatchlist([FromBody] WatchlistTvRequest request)
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

            if (request?.tvIds == null || !request.tvIds.Any())
            {
                return BadRequest("TV show IDs are required");
            }

            try
            {
                await this.userMediaService.AddTvShowsToWatchlist(userId, request.tvIds);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error adding TV shows to watchlist: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPost("tv/removeFromWatchlist")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> RemoveTvShowsFromWatchlist([FromBody] WatchlistTvRequest request)
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

            if (request?.tvIds == null || !request.tvIds.Any())
            {
                return BadRequest("TV show IDs are required");
            }

            try
            {
                await this.userMediaService.RemoveTvShowsFromWatchlist(userId, request.tvIds);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error removing TV shows from watchlist: {ex.Message}");
            }
        }

        [Authorize]
        [HttpGet("tv/favourites")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> GetFavouriteTvShows()
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

            try
            {
                var favourites = await this.userMediaService.GetUserFavouriteTvShows(userId);
                return Ok(favourites);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("tv/removeFromFavourites")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> RemoveTvShowsFromFavourites([FromBody] WatchlistTvRequest request)
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

            if (request?.tvIds == null || !request.tvIds.Any())
            {
                return BadRequest("TV show IDs are required");
            }

            try
            {
                await this.userMediaService.RemoveTvShowsFromFavourites(userId, request.tvIds);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
