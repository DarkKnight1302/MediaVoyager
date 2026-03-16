namespace MediaVoyager.Models.Dashboard
{
    public class ContentEngagementMetrics
    {
        public int TotalActions { get; set; }
        public double AvgActionsPerUser { get; set; }
        public double MoviePercentage { get; set; }
        public double TvPercentage { get; set; }
        public int NewUsersInPeriod { get; set; }
        public List<EngagementByType> EngagementByType { get; set; } = new List<EngagementByType>();
    }

    public class EngagementByType
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
