namespace MediaVoyager.Handlers
{
    public interface ISignInHandler
    {
        public Task SendOtpEmail(string email);

        public string VerifyOtpAndReturnAuthToken(string email, string otp);
    }
}
