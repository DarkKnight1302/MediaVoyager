using Newtonsoft.Json;
using System.Text.Json;
using TMDbLib.Objects.Search;

namespace MediaVoyager.ApiRequest
{
    public class AddUserTvRequest
    {
        [JsonProperty("tvShows")]
        public List<SearchTv> tvShows { get; set; }
    }
}