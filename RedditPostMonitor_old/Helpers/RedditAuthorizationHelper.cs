using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedditPostMonitor.Configuration;

namespace RedditPostMonitor.Helpers;

public class RedditAuthorizationHelper
{

    private readonly RedditMonitor _redditOptions;
    private readonly ILogger<App> _logger;


    public RedditAuthorizationHelper(IOptions<RedditMonitor> redditOptions, ILogger<App> logger)
    {

        _logger = logger;
        _redditOptions = redditOptions.Value;
    }

    public string GetAuthorizationUrl()
    {
        var parameters = new Dictionary<string, string?>
        {
            {"client_id", _redditOptions.ClientId},
            {"response_type", "code"},
            {"state", Guid.NewGuid().ToString()},
            {"redirect_uri", _redditOptions.CallBackUrl},
            {"duration", "permanent"},
            {"scope", "identity edit read submit"}
        };

        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={HttpUtility.UrlEncode(p.Value)}"));
        return $"{_redditOptions.AuthorizationEndPoint}?{queryString}";
    }

    public async Task<string> GetRefreshTokenAsync(string authorizationCode)
    {
        using (var client = new HttpClient())
        {
            // Set up basic authentication
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_redditOptions.ClientId}:{_redditOptions.ClientSecret}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

            // Prepare the token request
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"grant_type", "authorization_code"},
                {"code", authorizationCode},
                {"redirect_uri", _redditOptions.CallBackUrl!}
            });

            // Make the request
            var response = await client.PostAsync(_redditOptions.TokenEndPoint, content);
            var result = await response.Content.ReadAsStringAsync();

            // Parse the JSON response (you might want to use a proper JSON parser)
            // The response will contain the refresh_token
            return result;
        }
    }
    
    
    
}