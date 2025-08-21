namespace MediaVoyager.Entities
{
    public class UserTv
    {
        public string id { get; set; }

        public List<string> favouriteTv { get; set; } = new List<string>();

        public List<string> watchHistory { get; set; } = new List<string>();
    }
}
