namespace MediaVoyager.Models.Dashboard
{
    public class LoginMetrics
    {
        public int TotalLogins { get; set; }
        public int UniqueUsersLoggedIn { get; set; }
        public List<DateCount> LoginsByDate { get; set; } = new List<DateCount>();
    }
}
