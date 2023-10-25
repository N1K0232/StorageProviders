namespace StorageProviders;

public interface IStorageProvider : IDisposable
{
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> EnumerateAsync(string? prefix, string[] extensions, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> EnumerateAsync(string? prefix = null, params string[] extensions);

    Task<StorageFileInfo?> GetPropertiesAsync(string path, CancellationToken cancellationToken = default);

    Task<Stream?> ReadAsync(string path, CancellationToken cancellationToken = default);

    Task<byte[]?> ReadAsByteArrayAsync(string path, CancellationToken cancellationToken = default);

    Task<string?> ReadAsStringAsync(string path, CancellationToken cancellationToken = default);

    Task UploadAsync(string path, Stream stream, bool overwrite = false, CancellationToken cancellationToken = default);

    Task UploadAsync(string path, byte[] content, bool overwrite = false, CancellationToken cancellationToken = default);
}