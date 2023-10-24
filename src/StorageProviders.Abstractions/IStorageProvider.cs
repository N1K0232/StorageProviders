using StorageProviders.Abstractions;

namespace StorageProviders;

public interface IStorageProvider : IDisposable
{
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    Task<StorageFileInfo?> GetPropertiesAsync(string path, CancellationToken cancellationToken = default);

    Task<Stream?> ReadAsync(string path, CancellationToken cancellationToken = default);

    Task UploadAsync(string path, Stream stream, bool overwrite = false, CancellationToken cancellationToken = default);
}