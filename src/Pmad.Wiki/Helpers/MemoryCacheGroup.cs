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

        // Do not dispose the old CTS, as there may be a concurrent thread calling Set/GetOrCreate,
        // and disposing the CTS would cause ObjectDisposedExceptions. Let it be collected by GC when no longer referenced.
    }

    /// <summary>
    /// Singleton to protect against concurrent creation of multiple CTS for the same group, which would cause Clear to be ineffective.
    /// </summary>
    /// <remarks>
    /// Multiple <see cref="MemoryCacheGroup"/> instances can share the same underlying <see cref="IMemoryCache"/>
    /// and group key, regardless of their DI lifetime (scoped, singleton, etc.). Because
    /// <see cref="IMemoryCache.GetOrCreate{TItem}(object, Func{ICacheEntry,TItem})"/> is not atomic, we use a
    /// global lock to ensure that only a single <see cref="CancellationTokenSource"/> is created per group key.
    /// The contention should be minimal as the CTS is only created on cache misses, and very few cache groups are
    /// expected to be created.
    /// </remarks>
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
