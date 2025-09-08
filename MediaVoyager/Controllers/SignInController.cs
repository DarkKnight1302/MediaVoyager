using MediaVoyager.Constants;
using MediaVoyager.Handlers;
using MediaVoyager.Models;
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

        [HttpPost("verify-otp")]
        [RateLimit(3, 5)]
        public IActionResult VerifyOtp(VerifyOtpRequest verifyOtpRequest)
        {
            string email = HttpContext.Request.Headers["x-uid"].ToString();
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required");
            }
            string authToken = this.signInHandler.VerifyOtpAndReturnAuthToken(email, verifyOtpRequest.Otp);
            if (string.IsNullOrEmpty(authToken))
            {
                return BadRequest("Invalid OTP");
            }
            return Ok(authToken);
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
