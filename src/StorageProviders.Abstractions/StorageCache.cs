using Microsoft.Extensions.Caching.Memory;

namespace StorageProviders;

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
        if (cache.TryGetValue(path, out Stream? stream))
        {
            return Task.FromResult(stream);
        }

        return Task.FromResult<Stream?>(null);
    }

    public Task SetAsync(string path, Stream stream, TimeSpan expiration)
    {
        cache.Set(path, stream, expiration);
        return Task.CompletedTask;
    }
}