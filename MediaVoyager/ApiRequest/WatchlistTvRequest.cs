using System.ComponentModel.DataAnnotations;

namespace MediaVoyager.ApiRequest
{
    public class WatchlistTvRequest
    {
        [Required]
        public List<string> tvIds { get; set; } = new List<string>();
    }
}