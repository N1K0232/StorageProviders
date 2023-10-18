using Microsoft.Extensions.Caching.Memory;

namespace StorageProviders.Abstractions;

internal class StorageCache : IStorageCache
{
    private readonly IMemoryCache cache;

    public StorageCache(IMemoryCache cache)
    {
        this.cache = cache;
    }

    public Task DeleteAsync(string path)
    {
        cache.Remove(path);
        return Task.CompletedTask;
    }

    public Task<Stream?> GetAsync(string path)
    {
        var stream = cache.Get<Stream>(path);
        return Task.FromResult(stream);
    }

    public Task SetAsync(string path, Stream stream, TimeSpan expiration)
    {
        cache.Set(path, stream, expiration);
        return Task.CompletedTask;
    }
}