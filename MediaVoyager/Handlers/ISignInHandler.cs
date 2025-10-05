namespace MediaVoyager.Handlers
{
    public interface ISignInHandler
    {
        public Task SendOtpEmail(string email);

        public Task<string> VerifyOtpAndReturnAuthToken(string email, string otp);
    }
}
