using System.Collections.Concurrent;

namespace SubRedditMonitor.Services.Cache;

public class CacheRepository<TValue>
{
    private const int MaxNumberOfItems = 80;
    private readonly ConcurrentDictionary<RedditKey, TValue> _cache;

    public CacheRepository()
    {
        _cache = new ConcurrentDictionary<RedditKey, TValue>();
    }

    /// <summary>
    /// Adds a new value to the cache or updates the existing value associated with the specified Reddit name and date.
    /// </summary>
    /// <param name="redditName">The name of the subreddit.</param>
    /// <param name="itemReference"></param>
    /// <param name="value">The value to be added or updated in the cache.</param>
    public void AddOrUpdate(string redditName, Guid itemReference, TValue value)
    {
        var key = new RedditKey(redditName, itemReference);
        _cache.AddOrUpdate(key, value, (_, _) => value);

        // Check and remove oldest entries if needed
        RemoveOldestEntriesIfNeeded();
    }

    /// <summary>
    /// Attempts to retrieve a value from the cache associated with the specified Reddit name and date.
    /// </summary>
    /// <param name="redditName">The name of the subreddit.</param>
    /// <param name="itemReference"></param>
    /// <param name="value">When this method returns, contains the value associated with the specified Reddit name and date, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
    /// <returns>
    /// <c>true</c> if the cache contains an element with the specified Reddit name and date; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetValue(string redditName, Guid itemReference, out TValue? value)
    {
        var key = new RedditKey(redditName, itemReference);
        return _cache.TryGetValue(key, out value);
    }

    /// <summary>
    /// Removes the value associated with the specified Reddit name and date from the cache.
    /// </summary>
    /// <param name="redditName">The name of the subreddit.</param>
    /// /// <param name="itemReference"></param>
    /// <returns>
    /// <c>true</c> if the element is successfully removed; otherwise, <c>false</c>. 
    /// This method also returns <c>false</c> if the key was not found in the cache.
    /// </returns>
    public bool Remove(string redditName, Guid itemReference)
    {
        var key = new RedditKey(redditName, itemReference);
        return _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Method to remove the oldest entries if the cache has more than 20 items
    /// </summary>
    private void RemoveOldestEntriesIfNeeded()
    {
        if (_cache.Count <= MaxNumberOfItems)
            return;

        // Sort keys by date in ascending order
        var keysToRemove = _cache.Keys
            .OrderBy(key => key.IssueDate)
            .Take(_cache.Count - 20)
            .ToList();

        // Remove each of the oldest keys
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public ConcurrentDictionary<RedditKey, TValue> GetAllItems()
    {
        return new ConcurrentDictionary<RedditKey, TValue>(_cache);
    }
}