using StorageProviders.Abstractions;

namespace StorageProviders.FileSystem;

internal class FileSystemStorageProvider : IStorageProvider
{
    private readonly FileSystemStorageSettings fileSystemStorageSettings;
    private readonly IStorageCache cache;

    public FileSystemStorageProvider(FileSystemStorageSettings fileSystemStorageSettings, IStorageCache cache)
    {
        this.fileSystemStorageSettings = fileSystemStorageSettings;
        this.cache = cache;
    }

    public async Task DeleteAsync(string path)
    {
        var fullPath = CreatePath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            await cache.DeleteAsync(path).ConfigureAwait(false);
        }
    }

    public Task<bool> ExistsAsync(string path)
    {
        var fullPath = CreatePath(path);
        var exists = File.Exists(fullPath);

        return Task.FromResult(exists);
    }

    public async Task<StorageFileInfo?> GetPropertiesAsync(string path)
    {
        var stream = await ReadCoreAsync(path).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        var fileName = Path.GetFileName(path);
        var fileInfo = new FileInfo(fileName);

        var storageFileInfo = new StorageFileInfo(path)
        {
            Length = stream.Length,
            CreationDate = fileInfo.CreationTimeUtc,
            LastModifiedDate = fileInfo.LastWriteTimeUtc
        };

        return storageFileInfo;
    }

    public async Task<Stream?> ReadAsync(string path)
    {
        var fullPath = CreatePath(path);
        var stream = await cache.GetAsync(fullPath).ConfigureAwait(false);

        if (stream is null)
        {
            return await ReadCoreAsync(path).ConfigureAwait(false);
        }

        return stream;
    }

    public async Task UploadAsync(string path, Stream stream, bool overwrite = false)
    {
        var fullPath = CreatePath(path);
        await CreateDirectoryAsync(path).ConfigureAwait(false);

        using var outputStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        stream.Position = 0;

        await stream.CopyToAsync(outputStream).ConfigureAwait(false);
        await cache.SetAsync(path, outputStream, TimeSpan.FromHours(1)).ConfigureAwait(false);
    }

    private Task CreateDirectoryAsync(string path)
    {
        var fullPath = CreatePath(path);
        var directoryName = Path.GetDirectoryName(fullPath) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(directoryName) || !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        return Task.CompletedTask;
    }

    private string CreatePath(string path)
    {
        var fullPath = Path.Combine(fileSystemStorageSettings.StorageFolder, path);
        if (!Path.IsPathRooted(fullPath))
        {
            fullPath = Path.Combine(fileSystemStorageSettings.SiteRootFolder, fullPath);
        }

        return fullPath;
    }

    private Task<Stream?> ReadCoreAsync(string path)
    {
        var fullPath = CreatePath(path);
        var exists = File.Exists(fullPath);

        Stream? stream = exists ? File.OpenRead(fullPath) : null;
        return Task.FromResult(stream);
    }
}