namespace MediaVoyager.Entities
{
    public class ApiRequestLog
    {
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string Api { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string? UserId { get; set; }
        public string? Path { get; set; }
    }
}
