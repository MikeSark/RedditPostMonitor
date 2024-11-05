using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditPostMonitor.Helpers;

internal static class Constants
{
    /// <summary>
    /// The name used for <see cref="HttpClient" /> instances.
    /// </summary>
    public static string HttpClientName = $"RedditMonitor";

    /// <summary>
    /// Constants used to determine reddit <i>thing</i> kinds.
    /// </summary>
    public static class Kind
    {
        public const string Comment = "t1";
        public const string User = "t2";
        public const string Submission = "t3";
        public const string Message = "t4";
        public const string Subreddit = "t5";
        public const string MoreComments = "more";
    }
}