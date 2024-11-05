namespace RedditPostMonitor.Configuration.Interfaces;

public interface ISubRedditPost
{
    string SubReddit { get; set; }
    string Title { get; set; }
    int Ups { get; set; }
    string Name { get; set; }
    string Author { get; set; }
    
}