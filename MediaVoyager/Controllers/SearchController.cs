using MediaVoyager.Entities;
using MediaVoyager.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewHorizonLib.Attributes;
using NewHorizonLib.Services;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;

namespace MediaVoyager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly TMDbClient tmdbClient;
        private readonly IUserActivityRepository userActivityRepository;

        public SearchController(ISecretService secretService, IUserActivityRepository userActivityRepository)
        {
            string tmdbApiKey = secretService.GetSecretValue("tmdb_api_key");
            this.tmdbClient = new TMDbClient(tmdbApiKey);
            this.userActivityRepository = userActivityRepository;
        }

        [Authorize]
        [HttpGet("movies")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> SearchMovies(string keyword)
        {
            if (string.IsNullOrEmpty(keyword) || keyword.Length < 2)
            {
                return BadRequest();
            }

            string userId = HttpContext.Request.Headers["x-uid"].FirstOrDefault();

            SearchContainer<SearchMovie> movies = await this.tmdbClient.SearchMovieAsync(keyword);

            // Log movie search activity
            if (!string.IsNullOrEmpty(userId))
            {
                await userActivityRepository.LogActivityAsync(userId, ActivityTypes.MovieSearch, keyword);
            }

            if (movies.Results.Count == 0)
            {
                return NotFound();
            }
            return Ok(movies.Results);
        }

        [Authorize]
        [HttpGet("tvShows")]
        [RateLimit(100, 5)]
        public async Task<IActionResult> SearchTvShows(string keyword)
        {
            if (string.IsNullOrEmpty(keyword) || keyword.Length < 2)
            {
                return BadRequest();
            }

            string userId = HttpContext.Request.Headers["x-uid"].FirstOrDefault();

            SearchContainer<SearchTv> tvShows = await this.tmdbClient.SearchTvShowAsync(keyword);

            // Log TV search activity
            if (!string.IsNullOrEmpty(userId))
            {
                await userActivityRepository.LogActivityAsync(userId, ActivityTypes.TvSearch, keyword);
            }

            if (tvShows.Results.Count == 0)
            {
                return NotFound();
            }
            return Ok(tvShows.Results);
        }
    }
}
