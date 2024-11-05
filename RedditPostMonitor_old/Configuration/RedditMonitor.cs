using RedditPostMonitor.Configuration.Interfaces;

namespace RedditPostMonitor.Configuration;

public class RedditMonitor : IRedditMonitor
{
    public string? AppName { get; set; }
    public string? CallBackUrl { get; set; }
    public string? TokenEndPoint { get; set; }
    public string? AuthorizationEndPoint { get; set; }
    public string? BaseUrl { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? UserName { get; set; }
    public string? UserPassword { get; set; }
    public List<string> SubReddits { get; set; }
}