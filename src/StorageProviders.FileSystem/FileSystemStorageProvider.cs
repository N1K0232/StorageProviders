using StorageProviders.Abstractions;

namespace StorageProviders.FileSystem;

public class FileSystemStorageProvider : IStorageProvider
{
    private readonly FileSystemStorageSettings fileSystemStorageSettings;
    private readonly IStorageCache storageCache;

    public FileSystemStorageProvider(FileSystemStorageSettings fileSystemStorageSettings, IStorageCache storageCache)
    {
        this.fileSystemStorageSettings = fileSystemStorageSettings;
        this.storageCache = storageCache;
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = CreatePath(path);
        bool exists = File.Exists(fullPath);

        if (exists)
        {
            File.Delete(fullPath);
            await storageCache.DeleteAsync(path, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = CreatePath(path);
        bool exists = File.Exists(fullPath);

        return Task.FromResult(exists);
    }

    public async Task<StorageFileInfo?> GetPropertiesAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));
        cancellationToken.ThrowIfCancellationRequested();

        Stream? stream = await ReadCoreAsync(path, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        string? fileName = Path.GetFileName(path) ?? string.Empty;
        var fileInfo = new FileInfo(fileName);

        var storageFileInfo = new StorageFileInfo(path)
        {
            Length = stream.Length,
            CreationDate = fileInfo.CreationTimeUtc,
            LastModifiedDate = fileInfo.LastWriteTimeUtc
        };

        return storageFileInfo;
    }

    public async Task<Stream?> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = CreatePath(path);
        Stream? stream = await storageCache.ReadAsync(fullPath, cancellationToken).ConfigureAwait(false);

        if (stream is null)
        {
            return await ReadCoreAsync(path, cancellationToken).ConfigureAwait(false);
        }

        return stream;
    }

    public async Task UploadAsync(string path, Stream stream, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = CreatePath(path);
        await CreateDirectoryAsync(path, cancellationToken).ConfigureAwait(false);

        using var outputStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        stream.Position = 0;

        await stream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
        await storageCache.SetAsync(fullPath, outputStream, TimeSpan.FromHours(1), cancellationToken).ConfigureAwait(false);
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

    private Task<Stream?> ReadCoreAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = CreatePath(path);
        bool exists = File.Exists(fullPath);

        Stream? stream = exists ? File.OpenRead(fullPath) : null;
        return Task.FromResult(stream);
    }
}