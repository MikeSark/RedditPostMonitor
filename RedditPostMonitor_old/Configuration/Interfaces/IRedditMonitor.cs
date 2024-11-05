using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditPostMonitor.Configuration.Interfaces;

public interface IRedditMonitor
{
    string? AppName { get; set; }
    string? CallBackUrl { get; set; }
    string? TokenEndPoint { get; set; }
    string? AuthorizationEndPoint { get; set; }
    string? BaseUrl { get; set; }
    string? ClientId { get; set; }
    string? ClientSecret { get; set; }
    string? UserName { get; set; }
    string? UserPassword { get; set; }
    List<string> SubReddits { get; set; }
}