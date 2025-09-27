using System.ComponentModel.DataAnnotations;

namespace MediaVoyager.ApiRequest
{
    public class WatchlistMovieRequest
    {
        [Required]
        public List<string> movieIds { get; set; } = new List<string>();
    }
}