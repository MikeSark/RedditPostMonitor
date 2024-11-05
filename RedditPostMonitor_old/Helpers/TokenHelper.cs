using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedditPostMonitor.Configuration;

namespace RedditPostMonitor.Helpers;

public class TokenHelper
{
    private const string TokenEndPoint = "https://www.reddit.com/api/v1/access_token";

    private readonly HttpClient _httpClient;
    private readonly RedditMonitor _redditOptions;
    private readonly ILogger<TokenHelper> _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    private string _redditAccessToken;
    private string _tokenType;

    public DateTime IssueDateTime { get; private set; }
    public int TokenExpiresInSeconds { get; private set; }

    public TokenHelper(HttpClient httpClient, IOptions<RedditMonitor> redditOptions, ILogger<TokenHelper> logger)
    {
        _httpClient = httpClient;
        _redditOptions = redditOptions.Value;
        _logger = logger;
        
        _redditAccessToken = string.Empty;
        IssueDateTime = DateTime.MinValue;
        _tokenType = string.Empty;
    }

    public string GetToken()
    {
        _semaphore.Wait();
        try
        {
            var timeElapsed = DateTime.Now - IssueDateTime;
            if (IssueDateTime != DateTime.MinValue && timeElapsed.TotalMinutes < (12 * 59) && !string.IsNullOrWhiteSpace(_redditAccessToken))
            {
                return _redditAccessToken;
            }

            _redditAccessToken = GetTokenInternal().Result;
            IssueDateTime = DateTime.Now;

            return _redditAccessToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<string> GetTokenInternal()
    {
        // Create the request body
        var requestData = new Dictionary<string, string>
        {
            //{ "grant_type", "password" },
            { "username", _redditOptions.UserName!},
            { "password", _redditOptions.UserPassword! }
        };

        // Convert body to JSON
        var jsonContent = JsonSerializer.Serialize(requestData);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Add headers
        _httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(
                                                 Encoding.ASCII.GetBytes($"{_redditOptions.ClientId}:{_redditOptions.ClientSecret}")));

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RedditMonitor/1.0");
        // Make the POST request
        var response = await _httpClient.PostAsync(TokenEndPoint, content);

        var data = await response.Content.ReadAsStringAsync();
        return data;
    }
}
