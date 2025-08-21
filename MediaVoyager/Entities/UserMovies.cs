namespace MediaVoyager.Entities
{
    public class UserMovies
    {
        public string id { get; set; }

        public List<string> favouriteMovies { get; set; } = new List<string>();

        public List<string> watchHistory { get; set; } = new List<string>();
    }
}
