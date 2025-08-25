using TMDbLib.Objects.General;

namespace MediaVoyager.Models
{
    public class MovieResponse
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string Logo { get; set; }

        public List<Genre> Genres { get; set; }

        public string OriginCountry { get; set; }

        public string ReleaseDate { get; set; }

        public string Poster { get; set; }

        public string TagLine { get; set; }

        public string OverView { get; set; }
    }
}
