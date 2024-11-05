namespace SubRedditMonitor.Services.Cache;

public class RedditKey : IEquatable<RedditKey>
{
    public string RedditName { get; }
    public Guid Reference { get; }
    public DateTime IssueDate {get;}

    public RedditKey(string redditName, Guid reference)
    {
        RedditName = redditName ?? throw new ArgumentNullException(nameof(redditName));
        Reference = reference;
        IssueDate = DateTime.Now;
    }

    /// <summary>
    /// Determines whether the specified <see cref="RedditKey"/> is equal to the current <see cref="RedditKey"/>.
    /// </summary>
    /// <param name="other">The <see cref="RedditKey"/> to compare with the current <see cref="RedditKey"/>.</param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="RedditKey"/> is equal to the current <see cref="RedditKey"/>; otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(RedditKey? other)
    {
        if (other == null) return false;
        return RedditName == other.RedditName && Reference == other.Reference;
    }

    /// <summary>
    /// Returns a hash code for the current <see cref="RedditKey"/> instance.
    /// </summary>
    /// <returns>
    /// A hash code for the current <see cref="RedditKey"/> instance, which is a combination of the hash codes of the <see cref="RedditName"/> 
    /// </returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(RedditName, Reference);
    }

    
    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="RedditKey"/>.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="RedditKey"/>.</param>
    /// <returns>
    /// <c>true</c> if the specified object is equal to the current <see cref="RedditKey"/>; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as RedditKey);
    }
}