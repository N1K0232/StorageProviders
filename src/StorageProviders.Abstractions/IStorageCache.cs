namespace StorageProviders;

public interface IStorageCache : IDisposable
{
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    Task<Stream?> ReadAsync(string path, CancellationToken cancellationToken = default);

    Task SetAsync(string path, Stream stream, TimeSpan expiration, CancellationToken cancellationToken = default);
}