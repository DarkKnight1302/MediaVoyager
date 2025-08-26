using MediaVoyager.Constants;
using Microsoft.AspNetCore.Mvc;
using NewHorizonLib.Services.Interfaces;
using System.Security.Claims;

namespace MediaVoyager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SignInController : ControllerBase
    {
        private readonly ITokenService tokenService;

        public SignInController(ITokenService tokenService)
        {
            this.tokenService = tokenService;    
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
    }
}
