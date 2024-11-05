using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedditPostMonitor.Configuration.Interfaces;


namespace RedditPostMonitor;

// public class ReddItPostMonitoringService : IHostedLifecycleService
// {
//     
//     private readonly ILogger<ReddItPostMonitoringService> _logger;
//     private readonly IRedditMonitor _redditMonitorOptions ;
//     public ReddItPostMonitoringService(ILogger<ReddItPostMonitoringService> logger, IOptions<IRedditMonitor> redditOptions)
//     {
//         _logger = logger;
//         _redditMonitorOptions = redditOptions.Value;
//     }
//
//     public Task StartAsync(CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("StartAsync");
//         return Task.CompletedTask;
//     }
//
//     public Task StartingAsync(CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("StartingAsync");
//         return Task.CompletedTask;
//     }
//
//     public async Task StartedAsync(CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("StartedAsync");
//         while (!cancellationToken.IsCancellationRequested)
//         {
//             
//             
//             
//             
//             _logger.LogInformation($"In the Loop: {DateTime.Now:F}");
//             await Task.Delay(2000, cancellationToken);
//         }
//     }
//
//
//     public Task StopAsync(CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("StopAsync");
//         return Task.CompletedTask;
//     }
//
//     public Task StoppingAsync(CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("StoppingAsync");
//         return Task.CompletedTask;
//     }
//
//
//
//     public Task StoppedAsync(CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("StoppedAsync");
//         return Task.CompletedTask;
//     }
//
//     
// }