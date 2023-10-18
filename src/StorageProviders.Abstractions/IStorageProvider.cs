namespace StorageProviders;

public interface IStorageProvider
{
    Task DeleteAsync(string path);

    Task<bool> ExistsAsync(string path);

    Task<Stream?> ReadAsync(string path);

    Task UploadAsync(string path, Stream stream, bool overwrite = false);
}