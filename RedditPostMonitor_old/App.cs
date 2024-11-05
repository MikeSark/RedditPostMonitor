using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reddit.NET.Client.Builder;
using Reddit.NET.Client.Models.Public.Listings.Options;
using RedditPostMonitor.Configuration;
using RedditPostMonitor.Helpers;


namespace RedditPostMonitor;

public class App
{
    private readonly RedditMonitor _redditOptions;
    private readonly ILogger<App> _logger;
    private readonly RedditAuthorizationHelper _redditAuthorizationHelper;
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILoggerFactory _loggerFactory;

    private readonly TokenHelper _tokenHelper;

    public App(IOptions<RedditMonitor> redditOptions, ILogger<App> logger, TokenHelper tokenHelper,
               RedditAuthorizationHelper redditAuthorizationHelper, IHttpClientFactory clientFactory,
               ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _redditOptions = redditOptions.Value;
        _redditAuthorizationHelper = redditAuthorizationHelper;
        _clientFactory = clientFactory;
        _loggerFactory = loggerFactory;
        _tokenHelper = tokenHelper;
    }


    public async Task<string> RunAsync()
    {
        _logger.LogInformation("Starting the Reddit Monitoring Process");

        var client = await RedditClientBuilder
            .New
            .WithHttpClientFactory(_clientFactory)
            .WithLoggerFactory(_loggerFactory)
            .WithCredentialsConfiguration(credentialsBuilder => { ConfigureReadOnlyCredentials(credentialsBuilder); })
            .BuildAsync();

        var askReddit = client.Subreddit("askreddit");

        var askRedditDetails = await askReddit.GetDetailsAsync();

        _logger.LogInformation(askRedditDetails.ToString());

        var topOneHundredHotSubmissions = askReddit.GetSubmissionsAsync(builder => builder
                                                                       .WithSort(SubredditSubmissionSort.Hot)
                                                                       .WithMaximumItems(100));

        await foreach (var submission in topOneHundredHotSubmissions)
        {
            _logger.LogInformation(submission.ToString());
        }

        var me = client.Me();

        // This will fail as read-only mode does not have access to use details
        try
        {
            var meDetails = await me.GetDetailsAsync();

            _logger.LogInformation(meDetails.ToString());
        }
        catch (Exception)
        {
            _logger.LogWarning("Cannot interact with user when using read-only authentication mode.");
        }

        return "";
    }


    private void ConfigureReadOnlyCredentials(CredentialsBuilder credentialsBuilder)
    {
        var clientId = "bs1ioWTN-Un3FvU7siVsKg";
        var clientSecret = "3HQbbxmypMN2gWS9mcuUzXIMWbX39w";
        var username = "Work_Request";
        var password = "Work@PublicStorage";

        credentialsBuilder.Script(
            clientId,
            clientSecret,
            username,
            password);
        
        // credentialsBuilder.Script(
        //     _redditOptions.ClientId,
        //     _redditOptions.ClientSecret,
        //     _redditOptions.UserName,
        //     _redditOptions.UserPassword);
    }
}