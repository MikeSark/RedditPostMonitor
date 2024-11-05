using Microsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reddit.NET.Client.Builder;
using Reddit.NET.Client.Models.Public.Listings.Options;
using SubRedditMonitor.Configuration;
using SubRedditMonitor.Models;

namespace SubRedditMonitor.Services;

public class SubRedditProcessingService
{
    private readonly ILogger<SubRedditProcessingService> _logger;
    private readonly RedditMonitor _redditOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    
    private readonly List<SubmissionDetailsInternal> _submissionList = new List<SubmissionDetailsInternal>();
    public SubRedditProcessingService(IOptions<RedditMonitor> redditOptions,
                                      ILogger<SubRedditProcessingService> logger,
                                      ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
    {
        _logger = Requires.NotNull(logger, nameof(logger));
        _redditOptions = redditOptions.Value;

        _loggerFactory = loggerFactory;
        _httpClientFactory = httpClientFactory;
    }

    
    public async Task<List<SubmissionDetailsInternal>> StartAsync(string subRedditName, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Starting SubRedditProcessingService for subreddit {subRedditName}");
        try
        {
            _logger.LogInformation("Connecting to Reddit account");
            var client = await RedditClientBuilder
                .New
                .WithHttpClientFactory(_httpClientFactory)
                .WithLoggerFactory(_loggerFactory)
                .WithCredentialsConfiguration(credentialsBuilder =>
                    {
                        credentialsBuilder.Script(
                            _redditOptions.ClientId,
                            _redditOptions.ClientSecret,
                            _redditOptions.UserName,
                            _redditOptions.UserPassword);
                    })
                .BuildAsync(cancellationToken);

            var subReddit = client.Subreddit(_redditOptions.SubReddits[0]);
            var topOneHundredHotSubmissions = subReddit
                .GetSubmissionsAsync(builder =>
                                         builder
                                             .WithSort(SubredditSubmissionSort.New)
                                             .WithItemsPerRequest(100)
                                             .WithMaximumItems(_redditOptions.PostCount));

            await foreach (var submission in topOneHundredHotSubmissions)
            {
                _logger.LogDebug($"Author, Title, UpVotes: {submission.Author}, {submission.Title}, {submission.Upvotes}");
                _submissionList.Add(new SubmissionDetailsInternal(submission.Title, submission.Author, submission.Upvotes));
            }

            return _submissionList;
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogError($"SubRedditProcessingService for subreddit {exception.Message} was cancelled", exception);
            return new List<SubmissionDetailsInternal>();
        }
    }
}