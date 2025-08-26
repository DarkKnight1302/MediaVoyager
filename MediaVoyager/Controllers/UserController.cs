using MediaVoyager.ApiRequest;
using MediaVoyager.Constants;
using MediaVoyager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> AddFavouriteMovies([FromBody] AddUserMovieRequest addUserMovieRequest)
        {
            bool isValid = this.tokenService.IsValidAuth(addUserMovieRequest.userId, HttpContext, GlobalConstant.Issuer);

            if (!isValid)
            {
                return Unauthorized();
            }
            await this.userMediaService.AddMoviesToFavourites(addUserMovieRequest.userId, addUserMovieRequest.favouriteMovies);
            return Ok();
        }
    }
}
