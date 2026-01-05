using MediaVoyager.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewHorizonLib.Services.Interfaces;

namespace MediaVoyager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;

        public AuthController(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        [Authorize]
        [HttpGet("validate")]
        public IActionResult Validate()
        {
            string userId = HttpContext.Request.Headers["x-uid"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("User ID header is required");
            }

            bool isValid = _tokenService.IsValidAuth(userId, HttpContext, GlobalConstant.Issuer);
            return Ok(false);
        }
    }
}
