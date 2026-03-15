using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Test.Helpers;

public class MemoryCacheGroupTest
{
    private static MemoryCache CreateCache() =>
        new MemoryCache(Options.Create(new MemoryCacheOptions()));

    private static MemoryCacheGroup CreateGroup(IMemoryCache cache, string groupKey = "test-group") =>
        new MemoryCacheGroup(cache, groupKey, TimeSpan.FromMinutes(10));

    #region TryGetValue Tests

    [Fact]
    public void TryGetValue_WhenNotSet_ReturnsFalse()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache);

        var result = group.TryGetValue<string>("key", out var value);

        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_AfterSet_ReturnsTrueWithValue()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache);

        group.Set("key", "hello");

        var result = group.TryGetValue<string>("key", out var value);

        Assert.True(result);
        Assert.Equal("hello", value);
    }

    [Fact]
    public void TryGetValue_AfterGetOrCreate_ReturnsTrueWithValue()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache);

        group.GetOrCreate("key", () => "created");

        var result = group.TryGetValue<string>("key", out var value);

        Assert.True(result);
        Assert.Equal("created", value);
    }

    #endregion

    #region Set Tests

    [Fact]
    public void Set_ReturnsStoredValue()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache);

        var returned = group.Set("key", 42);

        Assert.Equal(42, returned);
    }

    [Fact]
    public void Set_OverwritesExistingValue()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache);

        group.Set("key", "first");
        group.Set("key", "second");

        group.TryGetValue<string>("key", out var value);

        Assert.Equal("second", value);
    }

    #endregion

    #region GetOrCreate Tests

    [Fact]
    public void GetOrCreate_WhenNotCached_InvokesFactory()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache);
        var factoryCallCount = 0;

        group.GetOrCreate("key", () => { factoryCallCount++; return "value"; });

        Assert.Equal(1, factoryCallCount);
    }

    [Fact]
    public void GetOrCreate_WhenCached_DoesNotInvokeFactoryAgain()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache);
        var factoryCallCount = 0;

        group.GetOrCreate("key", () => { factoryCallCount++; return "value"; });
        group.GetOrCreate("key", () => { factoryCallCount++; return "value"; });

        Assert.Equal(1, factoryCallCount);
    }

    [Fact]
    public void GetOrCreate_ReturnsFactoryValue()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache);

        var result = group.GetOrCreate("key", () => 99);

        Assert.Equal(99, result);
    }

    [Fact]
    public void GetOrCreate_SecondCall_ReturnsCachedValue()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache);
        var counter = 0;

        group.GetOrCreate("key", () => ++counter);
        var result = group.GetOrCreate("key", () => ++counter);

        Assert.Equal(1, result);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_AfterSet_EntriesAreNoLongerRetrievable()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache);

        group.Set("key1", "value1");
        group.Set("key2", "value2");

        group.Clear();

        Assert.False(group.TryGetValue<string>("key1", out _));
        Assert.False(group.TryGetValue<string>("key2", out _));
    }

    [Fact]
    public void Clear_AfterGetOrCreate_FactoryIsInvokedAgain()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache);
        var factoryCallCount = 0;

        group.GetOrCreate("key", () => { factoryCallCount++; return "value"; });
        group.Clear();
        group.GetOrCreate("key", () => { factoryCallCount++; return "value"; });

        Assert.Equal(2, factoryCallCount);
    }

    [Fact]
    public void Clear_DoesNotAffectEntriesInOtherGroup()
    {
        var cache = CreateCache();
        var groupA = CreateGroup(cache, "group-a");
        var groupB = CreateGroup(cache, "group-b");

        groupA.Set("key", "from-a");
        groupB.Set("key", "from-b");

        groupA.Clear();

        Assert.False(groupA.TryGetValue<string>("key", out _));
        Assert.True(groupB.TryGetValue<string>("key", out var bValue));
        Assert.Equal("from-b", bValue);
    }

    [Fact]
    public void Clear_DoesNotAffectUnrelatedRawCacheEntries()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache, "group");

        cache.Set("unrelated-key", "unrelated-value");
        group.Set("key", "group-value");

        group.Clear();

        Assert.Equal("unrelated-value", cache.Get<string>("unrelated-key"));
    }

    [Fact]
    public void Clear_WhenCalledMultipleTimes_DoesNotThrow()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache);

        group.Set("key", "value");

        var exception = Record.Exception(() =>
        {
            group.Clear();
            group.Clear();
            group.Clear();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Clear_WhenGroupIsEmpty_DoesNotThrow()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache);

        var exception = Record.Exception(() => group.Clear());

        Assert.Null(exception);
    }

    [Fact]
    public void Set_AfterClear_StoresNewValue()
    {
        var cache = CreateCache();
        var group = CreateGroup(cache);

        group.Set("key", "old");
        group.Clear();
        group.Set("key", "new");

        Assert.True(group.TryGetValue<string>("key", out var value));
        Assert.Equal("new", value);
    }

    #endregion

    #region Key Isolation Tests

    [Fact]
    public void TwoGroups_WithSameKey_DoNotInterfere()
    {
        var cache = CreateCache();
        var groupA = CreateGroup(cache, "group-a");
        var groupB = CreateGroup(cache, "group-b");

        groupA.Set("key", "value-a");

        Assert.False(groupB.TryGetValue<string>("key", out _));
    }

    [Fact]
    public void EntryKeyNamedLikeCtsKey_DoesNotCollideWithCts()
    {
        var cache = CreateCache();
        // The CTS key for groupKey "group" is "cts:group" in the raw IMemoryCache.
        // An entry with key "cts:group" in this group is stored as "group:cts:group" — no collision.
        var group = CreateGroup(cache, "group");

        group.Set("cts:group", "should-not-collide");

        Assert.True(group.TryGetValue<string>("cts:group", out var value));
        Assert.Equal("should-not-collide", value);

        // Clear must still work, proving the CTS was not overwritten by the entry
        group.Clear();

        Assert.False(group.TryGetValue<string>("cts:group", out _));
    }

    [Fact]
    public void TwoGroups_ClearOneGroup_OtherGroupCanStillBeCleared()
    {
        var cache = CreateCache();
        var groupA = CreateGroup(cache, "group-a");
        var groupB = CreateGroup(cache, "group-b");

        groupA.Set("key", "value-a");
        groupB.Set("key", "value-b");

        groupA.Clear();
        groupB.Clear();

        Assert.False(groupA.TryGetValue<string>("key", out _));
        Assert.False(groupB.TryGetValue<string>("key", out _));
    }

    #endregion
}
