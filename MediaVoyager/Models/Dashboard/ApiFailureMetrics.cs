namespace MediaVoyager.Models.Dashboard
{
    public class ApiFailureMetrics
    {
        public List<ApiDayFailureCount> FailuresByApiAndDate { get; set; } = new();
    }

    public class ApiDayFailureCount
    {
        public string Api { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}
