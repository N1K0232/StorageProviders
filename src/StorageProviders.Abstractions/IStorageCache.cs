namespace StorageProviders;

public interface IStorageCache
{
    Task DeleteAsync(string path);

    Task<Stream?> ReadAsync(string path);

    Task SetAsync(string path, Stream stream, TimeSpan expiration);
}