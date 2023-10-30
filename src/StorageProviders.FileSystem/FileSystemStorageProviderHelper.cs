namespace StorageProviders.FileSystem;

public sealed partial class FileSystemStorageProvider : StorageProvider, IStorageProvider
{
    private Task<bool> CheckExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string fullPath = CreatePath(path);

        bool exists = File.Exists(fullPath);
        return Task.FromResult(exists);
    }

    private Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = CreatePath(path);
        string directoryName = Path.GetDirectoryName(fullPath) ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(directoryName) || !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        return Task.CompletedTask;
    }

    private string CreatePath(string path)
    {
        string fullPath = Path.Combine(fileSystemStorageSettings.StorageFolder, path);
        if (!Path.IsPathRooted(fullPath))
        {
            fullPath = Path.Combine(fileSystemStorageSettings.SiteRootFolder, fullPath);
        }

        return fullPath;
    }

    private Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string fullPath = CreatePath(path);

        if (!string.IsNullOrWhiteSpace(fullPath) && Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
        }

        return Task.CompletedTask;
    }
}