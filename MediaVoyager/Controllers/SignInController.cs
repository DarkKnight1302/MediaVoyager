using MediaVoyager.Constants;
using MediaVoyager.Entities;
using MediaVoyager.Handlers;
using MediaVoyager.Models;
using MediaVoyager.Repositories;
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
        private readonly IUserActivityRepository userActivityRepository;

        public SignInController(ITokenService tokenService, ISignInHandler signInHandler, IUserActivityRepository userActivityRepository)
        {
            this.tokenService = tokenService;
            this.signInHandler = signInHandler;
            this.userActivityRepository = userActivityRepository;
        }

        [HttpPost("verify-otp")]
        [RateLimit(6, 1)]
        public async Task<IActionResult> VerifyOtp(VerifyOtpRequest verifyOtpRequest)
        {
            string email = HttpContext.Request.Headers["x-uid"].ToString();
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required");
            }
            (string authToken, bool setupRequired) = await this.signInHandler.VerifyOtpAndReturnAuthToken(email, verifyOtpRequest.Otp);
            if (string.IsNullOrEmpty(authToken))
            {
                return BadRequest("Invalid OTP");
            }
            
            // Log user login activity
            await this.userActivityRepository.LogActivityAsync(email, ActivityTypes.Login, "OTP Login");

            SignInResponse signInResponse = new SignInResponse
            {
                AuthToken = authToken,
                SetupRequired = setupRequired
            };
            return Ok(signInResponse);
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
