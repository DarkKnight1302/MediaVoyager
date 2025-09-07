namespace MediaVoyager.Handlers
{
    public interface ISignInHandler
    {
        public Task SendOtpEmail(string email);
    }
}
