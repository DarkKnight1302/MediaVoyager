using TMDbLib.Objects.General;

namespace MediaVoyager.Models
{
    public class TvShowResponse
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public List<Genre> Genres { get; set; }

        public string OriginCountry { get; set; }

        public DateTime? FirstAirDate { get; set; }

        public string Poster { get; set; }

        public string TagLine { get; set; }

        public string OverView { get; set; }

        public string OriginalName { get; set; }

        public int NumberOfSeasons { get; set; }
        }
}
