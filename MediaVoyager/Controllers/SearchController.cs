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
    [RateLimit(2, 1)]
    public class SearchController : ControllerBase
    {
        private readonly TMDbClient tmdbClient;

        public SearchController(ISecretService secretService)
        {
            string tmdbApiKey = secretService.GetSecretValue("tmdb_api_key");
            this.tmdbClient = new TMDbClient(tmdbApiKey);
        }

        [Authorize]
        [HttpGet("movies")]
        public async Task<IActionResult> SearchMovies(string keyword)
        {
            if (string.IsNullOrEmpty(keyword) || keyword.Length < 2)
            {
                return BadRequest();
            }
            SearchContainer<SearchMovie> movies = await this.tmdbClient.SearchMovieAsync(keyword);
            if (movies.Results.Count == 0)
            {
                return NotFound();
            }
            return Ok(movies.Results);
        }
    }
}
