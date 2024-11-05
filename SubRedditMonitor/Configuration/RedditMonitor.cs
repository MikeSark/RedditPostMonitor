using System.Collections.Generic;
using SubRedditMonitor.Configuration.Interfaces;

namespace SubRedditMonitor.Configuration;

public class RedditMonitor : IRedditMonitor
{
    public string? AppName { get; set; }
    public string? StatsEndPointBaseUrl { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? UserName { get; set; }
    public string? UserPassword { get; set; }
    public int PostCount { get; set; }
    public int ShowTopPosts { get; set; }
    public int PostRefreshInterval { get; set; }
    public List<string>? SubReddits { get; set; }
    public bool ShowResultInTerminal { get; set; }
}