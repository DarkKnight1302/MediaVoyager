namespace MediaVoyager.Models
{
    public class Movie : IEquatable<Movie>
    {
        public string Id { get; set;  }

        public string Title { get; set; }

        public DateTime? ReleaseDate { get; set; }

        public string Poster { get; set; }

        public string Overview { get; set; }

        public bool Equals(Movie? other)
        {
            if (other == null)
            {
                return false;
            }

            return this.Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Movie);
        }
    }
}
