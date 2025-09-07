using MediaVoyager.Constants;
using MediaVoyager.Handlers;
using Microsoft.AspNetCore.Mvc;
using NewHorizonLib.Attributes;
using NewHorizonLib.Services.Interfaces;
using System.Security.Claims;

namespace MediaVoyager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SignInController : ControllerBase
    {
        private readonly ITokenService tokenService;
        private readonly ISignInHandler signInHandler;

        public SignInController(ITokenService tokenService, ISignInHandler signInHandler)
        {
            this.tokenService = tokenService;
            this.signInHandler = signInHandler;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1234")
            };

            string token = this.tokenService.GenerateToken(claims, GlobalConstant.Issuer, "MediaVoyagerClient", 2);
            return Ok(token);
        }

        [HttpPost("send-otp")]
        [RateLimit(3, 10)]
        public async Task<IActionResult> SendOtp()
        {
            string email = HttpContext.Request.Headers["x-uid"].ToString();
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required");
            }
            await this.signInHandler.SendOtpEmail(email);
            return Ok();
        }
    }
}
