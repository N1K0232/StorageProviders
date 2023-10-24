using Microsoft.Extensions.Caching.Memory;

namespace StorageProviders;

internal class StorageCache : IStorageCache
{
    private IMemoryCache cache;
    private bool disposed = false;

    public StorageCache(IMemoryCache cache)
    {
        this.cache = cache;
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));
        cancellationToken.ThrowIfCancellationRequested();

        cache.Remove(path);
        return Task.CompletedTask;
    }

    public Task<Stream?> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));
        cancellationToken.ThrowIfCancellationRequested();

        if (cache.TryGetValue(path, out Stream? stream))
        {
            return Task.FromResult(stream);
        }

        return Task.FromResult<Stream?>(null);
    }

    public Task SetAsync(string path, Stream stream, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));
        cancellationToken.ThrowIfCancellationRequested();

        cache.Set(path, stream, expiration);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            if (cache != null)
            {
                cache.Dispose();
                cache = null!;
            }

            disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}