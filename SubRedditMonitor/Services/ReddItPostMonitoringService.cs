using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SubRedditMonitor.Configuration;
using SubRedditMonitor.Models;
using SubRedditMonitor.Services.Cache;

namespace SubRedditMonitor.Services;

public class ReddItPostMonitoringService : IHostedLifecycleService
{
    private readonly ILogger<ReddItPostMonitoringService> _logger;
    private readonly RedditMonitor _redditOptions;

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TaskManagementService _taskManager;
    private readonly CacheRepository<List<SubmissionDetailsInternal>> _cacheRepository;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public ReddItPostMonitoringService(ILogger<ReddItPostMonitoringService> logger, IOptions<RedditMonitor> redditOptions,
                                       IServiceScopeFactory serviceScopeFactory, TaskManagementService taskManager,
                                       IHostApplicationLifetime applicationLifetime,
                                       CacheRepository<List<SubmissionDetailsInternal>> cacheRepository)
    {
        _logger = logger;
        _redditOptions = redditOptions.Value;
        _serviceScopeFactory = serviceScopeFactory;
        _taskManager = taskManager;
        _applicationLifetime = applicationLifetime;
        _cacheRepository = cacheRepository;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("StartAsync");
        return Task.CompletedTask;
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("StartingAsync");
        return Task.CompletedTask;
    }

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("StartedAsync");

        if (_redditOptions.SubReddits == null)
        {
            _logger.LogInformation($"There are no subreddit that needs to be watched. Terminating process.");
            _applicationLifetime.StopApplication();

            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var subReddit in _redditOptions.SubReddits.Distinct())
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedService = scope.ServiceProvider.GetRequiredService<SubRedditProcessingService>();

                _taskManager.EnqueueTask<List<SubmissionDetailsInternal>>(async () =>
                    {
                        var result = await scopedService.StartAsync(subReddit, cancellationToken);
                        return result;
                    }, (response, exception) =>
                    {
                        if (exception != null)
                        {
                            _logger.LogError($"Retrieved {exception.Message} records", exception);
                            return;
                        }

                        // add data to global cache
                        //_cacheRepository.AddOrUpdate(subReddit, DateTime.Now, response);
                        _cacheRepository.AddOrUpdate(subReddit, Guid.NewGuid(), response);

                        // print data to screen if indicated so in appsettings.json
                        if (_redditOptions.ShowResultInTerminal)
                        {
                            var submissionReporter = new SubmissionReporterService();

                            // print data by Upvote
                            var filteredByUpVote = response.OrderByDescending(x => x.Upvotes).Take(_redditOptions.ShowTopPosts).ToList();
                            var headerLine = $"Report: {subReddit}{Environment.NewLine} Top {_redditOptions.ShowTopPosts} Posts with highest Upvote.";
                            submissionReporter.DisplaySubmissionsInConsole(headerLine, filteredByUpVote);


                            // print data by most posts by author
                            headerLine = $" Top {_redditOptions.ShowTopPosts} Posts with highest submission by user.";
                            submissionReporter.DisplayAuthorListInConsole(headerLine,
                                                                          filteredByUpVote.GroupBy(s => s.Author).Select(g => new SubmissionByAuthorInternal(g.Count(), g.Key)).ToList());
                        }
                    });
            }

            await _taskManager.WaitAllTasksAsync();

            // wait xx seconds and retry again..
            await Task.Delay((_redditOptions.PostRefreshInterval * 1000), cancellationToken);

            _logger.LogDebug($"In the Loop: {DateTime.Now:F}");
        }
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("StopAsync");
        return Task.CompletedTask;
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("StoppingAsync");
        return Task.CompletedTask;
    }


    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("StoppedAsync");
        return Task.CompletedTask;
    }
}