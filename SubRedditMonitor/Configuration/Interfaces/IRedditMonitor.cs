using System.Collections.Generic;

namespace SubRedditMonitor.Configuration.Interfaces;

public interface IRedditMonitor
{
    string? AppName { get; set; }
    string? StatsEndPointBaseUrl { get; set; }
    string? ClientId { get; set; }
    string? ClientSecret { get; set; }
    string? UserName { get; set; }
    string? UserPassword { get; set; }
    int PostCount { get; set; }
    int ShowTopPosts { get; set; }
    int PostRefreshInterval { get; set; }
    List<string> SubReddits { get; set; }
    bool ShowResultInTerminal { get; set; }

}