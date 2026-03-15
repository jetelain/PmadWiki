using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Pmad.Wiki.Helpers;

/// <summary>
/// Wraps a named group of <see cref="IMemoryCache"/> entries that can all be evicted together
/// via <see cref="Clear"/>, without affecting other entries in the shared cache.
/// </summary>
internal sealed class MemoryCacheGroup
{
    private readonly IMemoryCache _cache;
    private readonly string _keyPrefix;
    private readonly string _ctsKey;
    private readonly TimeSpan _slidingExpiration;

    public MemoryCacheGroup(IMemoryCache cache, string groupKey, TimeSpan slidingExpiration)
    {
        _cache = cache;
        _keyPrefix = groupKey + ":";
        _ctsKey = "cts:" + groupKey;
        _slidingExpiration = slidingExpiration;
    }

    public bool TryGetValue<TValue>(string key, out TValue? value)
    {
        return _cache.TryGetValue(_keyPrefix + key, out value);
    }

    public TValue Set<TValue>(string key, TValue value)
    {
        var entryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(_slidingExpiration)
            .AddExpirationToken(new CancellationChangeToken(GetOrCreateCts().Token));
        return _cache.Set(_keyPrefix + key, value, entryOptions);
    }

    public TValue GetOrCreate<TValue>(string key, Func<TValue> factory)
    {
        return _cache.GetOrCreate(_keyPrefix + key, entry =>
        {
            entry.SetSlidingExpiration(_slidingExpiration);
            entry.AddExpirationToken(new CancellationChangeToken(GetOrCreateCts().Token));
            return factory();
        })!;
    }

    public void Clear()
    {
        var old = _cache.Get<CancellationTokenSource>(_ctsKey);
        _cache.Remove(_ctsKey);
        old?.Cancel();
        old?.Dispose();
    }

    private static readonly object _ctsCreationLock = new();

    private CancellationTokenSource GetOrCreateCts()
    {
        // IMemoryCache.GetOrCreate is not atomic, so we need to do double-checked locking to ensure only one CTS is created per group

        if (_cache.TryGetValue(_ctsKey, out CancellationTokenSource? cts))
        {
            return cts!;
        }

        lock (_ctsCreationLock)
        {
            if (_cache.TryGetValue(_ctsKey, out cts))
            {
                return cts!;
            }

            cts = new CancellationTokenSource();
            _cache.Set(_ctsKey, cts, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });
            return cts;
        }
    }
}
