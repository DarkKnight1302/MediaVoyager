using MediaVoyager.Constants;
using MediaVoyager.Repositories;
using NewHorizonLib.Services.Interfaces;
using System.Security.Claims;

namespace MediaVoyager.Handlers
{
    public class SignInHandler : ISignInHandler
    {
        private readonly IEmailService _emailService;
        private readonly IOtpService _otpService;
        private readonly ITokenService _tokenService;
        private readonly IUserRepository _userRepository;
        private readonly IUserMoviesRepository _userMoviesRepository;
        private readonly IUserTvRepository _userTvRepository;

        public SignInHandler(IEmailService emailService,
            IOtpService otpService,
            ITokenService tokenService,
            IUserRepository userRepository,
            IUserMoviesRepository userMoviesRepository,
            IUserTvRepository userTvRepository)
        {
            _emailService = emailService;
            _otpService = otpService;
            _tokenService = tokenService;
            this._userRepository = userRepository;
            this._userMoviesRepository = userMoviesRepository;
            this._userTvRepository = userTvRepository;
        }

        public async Task SendOtpEmail(string email)
        {
            string otp = _otpService.GenerateOtp(email);
            string otpEmailBody = OtpEmailTemplate(otp);
            await _emailService.SendMail(email, otpEmailBody, "Your Media Voyager Verification Code", "MediaVoyager", "noreply@mediavoyager.in", true);
        }

        public async Task<(string, bool)> VerifyOtpAndReturnAuthToken(string email, string otp)
        {
            bool isValid = this._otpService.ValidateOtp(email, otp);
            if (!isValid)
            {
                return (string.Empty, true);
            }
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, email)
            };
            string token = this._tokenService.GenerateToken(claims, GlobalConstant.Issuer, "MediaVoyagerClient", 2000);
            var user = await this._userRepository.GetUser(email).ConfigureAwait(false);
            if (user == null)
            {
                await this._userRepository.CreateUser(email, email, false, email).ConfigureAwait(false);
            }
            var movies = await this._userMoviesRepository.GetUserMovies(email).ConfigureAwait(false);
            var tvShows = await this._userTvRepository.GetUserTv(email).ConfigureAwait(false);
            bool setupRequired = movies == null || tvShows == null || movies.favouriteMovies.Count == 0 || tvShows.favouriteTv.Count == 0;
            
            return (token, setupRequired);
        }

        private string OtpEmailTemplate(string otp)
        {
            // Professional dark-theme HTML optimized for email clients
            string emailTemplate = "<!DOCTYPE html>\r\n<html lang=\"en\" style=\"margin:0;padding:0;\">\r\n<head>\r\n <meta charset=\"UTF-8\">\r\n <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\r\n <title>Media Voyager - Email Verification</title>\r\n <style>\r\n body { margin:0; padding:0; background:#0b1020; -webkit-text-size-adjust:100%; -ms-text-size-adjust:100%; font-family: Arial, Helvetica, sans-serif; }\r\n .container { width:100%; background:#0b1020; }\r\n .wrap { max-width:600px; margin:0 auto; }\r\n .card { background:#111827; border-radius:12px; overflow:hidden; border:1px solid #2b3645; }\r\n .header { text-align:center; padding:24px16px8px16px; }\r\n .brand { color:#a1b5d8; letter-spacing:1.8px; font-size:12px; font-weight:700; }\r\n .title { color:#e6edf3; font-size:22px; font-weight:700; margin:10px000; }\r\n .hero { text-align:center; padding:10px16px16px; background:#0b1020; }\r\n .hero img { display:block; margin:0 auto; max-width:100%; height:auto; border:0; }\r\n .content { padding:24px20px8px20px; text-align:center; color:#d1d7e0; }\r\n .desc { font-size:14px; line-height:1.6; color:#b8c2cf; margin:0 auto; max-width:520px; }\r\n .otp { background:#0f172a; border:1px solid #334155; border-radius:10px; padding:20px; margin:18px auto8px auto; max-width:520px; }\r\n .otp-label { font-size:13px; font-weight:600; color:#c7d0dd; margin-bottom:8px; }\r\n .otp-code { font-family:Consolas, \"Courier New\", Courier, monospace; font-size:30px; font-weight:900; color:#f8fafc; letter-spacing:6px; }\r\n .otp-expiry { font-size:12px; color:#93a4b6; margin-top:8px; }\r\n .footer { background:#0f1426; padding:18px16px24px16px; text-align:center; border-top:1px solid #2b3645; }\r\n .footer-text { font-size:12px; color:#8ea2b7; line-height:1.5; }\r\n @media only screen and (max-width:600px) { .wrap { margin:10px; } .content { padding:18px16px; } .otp-code { font-size:26px; letter-spacing:5px; } }\r\n </style>\r\n</head>\r\n<body>\r\n <div style=\"display:none!important;visibility:hidden;opacity:0;color:transparent;height:0;width:0;overflow:hidden;mso-hide:all;line-height:0;max-height:0;max-width:0;\">\r\n Your verification code for Media Voyager is below. It expires in10 minutes.\r\n </div>\r\n <table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" class=\"container\">\r\n <tr>\r\n <td align=\"center\">\r\n <div class=\"wrap\">\r\n <div class=\"card\" role=\"article\" aria-roledescription=\"email\">\r\n <div class=\"header\">\r\n <div class=\"brand\">MEDIA VOYAGER</div>\r\n <div class=\"title\">Verify your email</div>\r\n </div>\r\n <div class=\"hero\" role=\"img\" aria-label=\"Media Voyager banner\">\r\n <img src=\"https://newhorizonblobstorage.blob.core.windows.net/media-voyager/frame_213_delay-0.04s.png\" height=\"200\" width=\"200\" alt=\"Media Voyager\">\r\n </div>\r\n <div class=\"content\">\r\n <p class=\"desc\">Use the 6-digit code below to finish signing in to Media Voyager.</p>\r\n <div class=\"otp\" role=\"group\" aria-label=\"Verification code\">\r\n <div class=\"otp-label\">Your verification code</div>\r\n <div class=\"otp-code\">{{OTP_CODE}}</div>\r\n <div class=\"otp-expiry\">This code expires in 10 minutes.</div>\r\n </div>\r\n </div>\r\n <div class=\"footer\">\r\n <div class=\"footer-text\">If you didn’t request this, you can safely ignore this email.</div>\r\n </div>\r\n </div>\r\n </div>\r\n </td>\r\n </tr>\r\n </table>\r\n</body>\r\n</html>";

            return emailTemplate.Replace("{{OTP_CODE}}", otp);
        }
    }
}
