namespace MediaVoyager.Models.Dashboard
{
    public class UserRetentionMetrics
    {
        public double RetentionRate { get; set; }
        public int ReturningUsers { get; set; }
        public List<DateCount> ReturningUsersByDate { get; set; } = new List<DateCount>();
    }
}
