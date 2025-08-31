using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TMDbLib.Utilities.Serializer;

namespace TMDbLib.Rest;

internal class RestResponse
{
    private readonly RestSharp.RestResponse Response;

    public RestResponse(RestSharp.RestResponse response)
    {
        Response = response;
    }

    public bool IsValid => Response != null;

    public HttpStatusCode StatusCode => Response.StatusCode;

    public string GetContent()
    {
        return Response.Content;
    }

    public string GetHeader(string name, string @default = null)
    {
        var header = Response.Headers.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        // Return the header value if found, otherwise return the default value
        return header?.Value?.ToString() ?? @default;
    }
}

internal class RestResponse<T> : RestResponse
{
    private readonly RestClient _client;

    public RestResponse(RestSharp.RestResponse response, RestClient client)
        : base(response)
    {
        _client = client;
    }

    public async Task<T> GetDataObject()
    {
        string content = GetContent();
        return JsonConvert.DeserializeObject<T>(content);
    }
}