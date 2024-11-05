using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedditPostMonitor;
using RedditPostMonitor.Configuration;
using RedditPostMonitor.Helpers;
using Serilog;



Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(ConfigureAppConfiguration)
    .ConfigureLogging(ConfigureLogging)
    .ConfigureServices(ConfigureServices)
    .Build()
    .Services.GetService<App>()
    ?.RunAsync()
    .GetAwaiter()
    .GetResult();


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
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            shared: true,
            flushToDiskInterval: TimeSpan.FromSeconds(1),
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    builder.ClearProviders();
    builder.AddSerilog(serilogLogger, dispose: true);
}


static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
{
    var redditOptions = new RedditMonitor();
    var eodConfigurationSection = hostContext.Configuration.GetSection("ReddItMonitor");

    services.Configure<RedditMonitor>(eodConfigurationSection);
    eodConfigurationSection.Bind(redditOptions);


    services.AddRedditHttpClient(userAgent: $"{Environment.OSVersion.Platform}:Reddit.NET.Console:v0.1.0 (by JedS6391)");

    services.AddOptions<RedditMonitor>()
        .Bind(hostContext.Configuration.GetSection("ReddItMonitor"))
        .Validate(options => !string.IsNullOrEmpty(options.ClientId), "Client ID must not be empty.")
        .Validate(options => !string.IsNullOrEmpty(options.ClientSecret), "Client secret must not be empty.")
        .Validate(options => !string.IsNullOrEmpty(options.UserName), "User Name must not be empty.")
        .Validate(options => !string.IsNullOrEmpty(options.UserPassword), "User Password must not be empty.");

    
    services.AddSingleton<RedditAuthorizationHelper>();
    services.AddSingleton<TokenHelper>();
    //services.AddHostedService<ReddItPostMonitoringService>();

    services.AddSingleton<App>();
}