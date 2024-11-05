using Microsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reddit.NET.Client.Builder;
using Reddit.NET.Client.Models.Public.Listings.Options;
using SubRedditMonitor.Configuration;
using SubRedditMonitor.Models;

namespace SubRedditMonitor
{
    /// <summary>
    /// Contains the logic to parse command line args and run examples.
    /// </summary>
    internal sealed class App
    {
        private readonly ILogger<App> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly RedditMonitor _redditOptions;

        private readonly List<SubmissionDetailsInternal> _submissionList = new List<SubmissionDetailsInternal>();

        public App(IOptions<RedditMonitor> redditOptions, ILogger<App> logger, ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
        {
            _logger = Requires.NotNull(logger, nameof(logger));
            _loggerFactory = Requires.NotNull(loggerFactory, nameof(loggerFactory));
            _httpClientFactory = Requires.NotNull(httpClientFactory, nameof(httpClientFactory));
            _redditOptions = redditOptions.Value;
        }

        public async Task RunAsync()
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
                .BuildAsync();


            var subReddit = client.Subreddit(_redditOptions.SubReddits[0]);
            var appleSubReddit = await subReddit.GetDetailsAsync();

            _logger.LogDebug($"sub reddit subs count: {appleSubReddit.Subscribers}");

            var topOneHundredHotSubmissions = subReddit
                .GetSubmissionsAsync(builder =>
                                         builder
                                             .WithSort(SubredditSubmissionSort.New)
                                             .WithItemsPerRequest(100)
                                             .WithMaximumItems(_redditOptions.PostCount));

            _logger.LogInformation($"Updating list of posts for subreddit :{_redditOptions.SubReddits[0]}");
            await foreach (var submission in topOneHundredHotSubmissions)
            {
                _logger.LogDebug($"Author, Title, UpVotes: {submission.Author}, {submission.Title}, {submission.Upvotes}");
                _submissionList.Add(new SubmissionDetailsInternal(submission.Title, submission.Author, submission.Upvotes));
            }

            Console.WriteLine($"{Environment.NewLine}---------------------------------------------------");
            Console.WriteLine($"Number of Posts read: {_submissionList.Count}");
            Console.WriteLine($"---------------------------------------------------{Environment.NewLine}{Environment.NewLine}");

            // display data by upvotes
            DisplayUserUpVotes(_submissionList);


            // display data by by user postings
            DisplayUserPostings(_submissionList);
        }

        private void DisplayUserPostings(List<SubmissionDetailsInternal> submissionList)
        {
            _logger.LogInformation($"{Environment.NewLine}Report: Posts sorteed bu number of posts by Author");
            _logger.LogInformation($"---------------------------------------------------");

            var submissionListSortedByAuthor = _submissionList
                .GroupBy(s => s.Author)
                .Select(g => new
                {
                    Author = g.Key,
                    PostCount = g.Count(),
                    Posts = g.ToList()
                })
                .OrderByDescending(g => g.PostCount)
                .Take(_redditOptions.ShowTopPosts)
                .ToList();

            foreach (var submission in submissionListSortedByAuthor)
            {
                _logger.LogInformation($"Author: {submission.Author,20}, Post Count: {submission.PostCount} ");
            }

            Console.WriteLine($"{Environment.NewLine}");
        }

        private void DisplayUserUpVotes(List<SubmissionDetailsInternal> submissionList)
        {
            _logger.LogInformation($"{Environment.NewLine}Report: Posts sorteed bu number of posts by Author");
            _logger.LogInformation($"---------------------------------------------------");
            var submissionListSortedByUpVotes = _submissionList.OrderByDescending(x => x.Upvotes).Take(_redditOptions.ShowTopPosts);

            _logger.LogInformation("10 top posts");
                foreach (var submission in submissionListSortedByUpVotes)
                {
                    _logger.LogInformation($"UpVotes, Author, Title: {submission.Upvotes,10}, {submission.Author,20}, {submission.Title}");
                }

            Console.WriteLine($"{Environment.NewLine}");
        }
    }
}