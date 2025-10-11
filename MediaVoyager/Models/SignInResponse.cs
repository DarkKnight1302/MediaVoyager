namespace MediaVoyager.Models
{
    public class SignInResponse
    {
        public string AuthToken { get; set; }
        public bool SetupRequired { get; set; }
    }
}
