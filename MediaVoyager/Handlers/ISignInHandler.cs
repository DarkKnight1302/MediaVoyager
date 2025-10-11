namespace MediaVoyager.Handlers
{
    public interface ISignInHandler
    {
        public Task SendOtpEmail(string email);

        public Task<(string, bool)> VerifyOtpAndReturnAuthToken(string email, string otp);
    }
}
