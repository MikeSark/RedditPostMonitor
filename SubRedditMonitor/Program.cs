using System.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reddit.NET.Client.Builder;
using Serilog;
using Serilog.Events;
using SubRedditMonitor.Configuration;
using SubRedditMonitor.Models;
using SubRedditMonitor.Services;
using SubRedditMonitor.Services.Cache;
using SubRedditMonitor.Services.EndPointServices;


var app = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(ConfigureAppConfiguration)
    .ConfigureLogging(ConfigureLogging)
    .ConfigureServices(ConfigureServices)
    .ConfigureWebHostDefaults(wb =>
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var redditOptions = new RedditMonitor();
            configuration.GetSection("ReddItMonitor").Bind(redditOptions);


            wb.UseUrls(redditOptions.StatsEndPointBaseUrl!);
            wb.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/stats", async context =>
                                {
                                    var cacheService = context.RequestServices.GetRequiredService<CacheRepository<List<SubmissionDetailsInternal>>>();
                                    var options = context.RequestServices.GetRequiredService<IOptions<RedditMonitor>>();

                                    await context.Response.WriteAsync(new EndPointServiceListService(cacheService, options).CreateHtmlStringForListOfCacheEnteries());
                                });

                            endpoints.MapGet("/stats/{subreddit}/{guidAsString}", async (string subreddit, string guidAsString, HttpContext context) =>
                                {
                                    var reference = Guid.Empty;
                                    if (Guid.TryParse(guidAsString, out var guid))
                                    {
                                        reference = guid;
                                    }


                                    var cacheService = context.RequestServices.GetRequiredService<CacheRepository<List<SubmissionDetailsInternal>>>();
                                    var options = context.RequestServices.GetRequiredService<IOptions<RedditMonitor>>();

                                    await context.Response.WriteAsync(new EndPointServiceListService(cacheService, options).CreateHtmlStringForCacheItem(subreddit, reference));
                                });
                        });
                });
        })
    .Build();

await app.RunAsync();


static void ConfigureAppConfiguration(HostBuilderContext hostContext, IConfigurationBuilder configuration)
{
    var env = hostContext.HostingEnvironment;

    configuration.SetBasePath(Directory.GetCurrentDirectory());
    configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

    configuration.AddEnvironmentVariables();
}


static void ConfigureLogging(HostBuilderContext hostContext, ILoggingBuilder builder)
{
    var serilogLogger = new LoggerConfiguration()
        .MinimumLevel.Debug() // Set the minimum log level
        .Enrich.FromLogContext()
        .WriteTo.Logger(lc => lc.Filter.ByIncludingOnly(evt => evt.Level == LogEventLevel.Information)
                            .Filter.ByExcluding(e => e.Properties.ToString().Contains("Microsoft"))
                            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Fatal)
                            .MinimumLevel.Override("Microsoft.Hosting", LogEventLevel.Fatal)
                            .MinimumLevel.Override("Microsoft", LogEventLevel.Fatal)
                            .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
        .WriteTo.Logger(lc => lc
                            .Filter.ByIncludingOnly(evt => evt.Level is LogEventLevel.Debug or > LogEventLevel.Information)
                            .Filter.ByExcluding(e => e.Properties.ToString().Contains("Microsoft"))
                            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Fatal)
                            .MinimumLevel.Override("Microsoft.Hosting", LogEventLevel.Fatal)
                            .MinimumLevel.Override("Microsoft", LogEventLevel.Fatal)
                            .WriteTo.File("logs/app-.log",
                                          rollingInterval: RollingInterval.Day,
                                          retainedFileCountLimit: 7,
                                          shared: true,
                                          flushToDiskInterval: TimeSpan.FromSeconds(1),
                                          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
        .CreateLogger();


    builder.ClearProviders();
    builder.AddSerilog(serilogLogger, dispose: true);
}


static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
{
    var redditOptions = new RedditMonitor();
    var configurationSection = hostContext.Configuration.GetSection("ReddItMonitor");

    services.Configure<RedditMonitor>(configurationSection);
    configurationSection.Bind(redditOptions);


    services.AddRedditHttpClient(userAgent: $"{Environment.OSVersion.Platform}:Reddit.NET.Console:v0.1.0 (by JedS6391)");

    services.AddOptions<RedditMonitor>()
        .Bind(hostContext.Configuration.GetSection("ReddItMonitor"))
        .Validate(op => !string.IsNullOrEmpty(op.ClientId), "Client ID must not be empty.")
        .Validate(op => !string.IsNullOrEmpty(op.ClientSecret), "Client secret must not be empty.")
        .Validate(op => !string.IsNullOrEmpty(op.UserName), "User Name must not be empty.")
        .Validate(op => !string.IsNullOrEmpty(op.UserName), "Base URL for Stats end point cannot be empty.")
        .Validate(op => !string.IsNullOrEmpty(op.UserPassword), "User Password must not be empty.");


    services.AddRedditHttpClient(userAgent: $"{Environment.OSVersion.Platform}:Reddit Monitor:v0.1.0 (by Work_Request)");
    services.AddSingleton<CacheRepository<List<SubmissionDetailsInternal>>>();
    services.AddHostedService<ReddItPostMonitoringService>();
    services.AddScoped<TaskManagementService>();
    services.AddScoped<SubRedditProcessingService>();

    DisplayCacheEndpoints(redditOptions);
}

static void DisplayCacheEndpoints(RedditMonitor redditOptions)
{
    // Show the end point for cache Items
    Console.WriteLine("┌" + new string('─', 100 - 2) + "┐");
    Console.WriteLine($"| {(new string(' ', 100 - 3))}|");
    Console.WriteLine($"| \tCache Items End Points: {(new string(' ', 68 - 1))}|");
    Console.WriteLine($"| \t\t {redditOptions.StatsEndPointBaseUrl!}/stats   to see the cache content{(new string(' ', 30 - 2))}|");
    Console.WriteLine($"| \t\t {redditOptions.StatsEndPointBaseUrl!}/stats/subreddit/guidAsString   to see the cache content{(new string(' ', 7 - 2))}|");
    Console.WriteLine($"| {(new string(' ', 100 - 3))}|");
    Console.WriteLine("└" + new string('─', 100 - 2) + "┘");
    Console.WriteLine();
}