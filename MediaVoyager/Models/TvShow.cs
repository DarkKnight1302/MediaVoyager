namespace MediaVoyager.Models
{
    public class TvShow : IEquatable<TvShow>
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public bool Equals(TvShow? other)
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
            return Equals(obj as TvShow);
        }
    }
}
