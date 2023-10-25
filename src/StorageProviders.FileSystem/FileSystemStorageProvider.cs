namespace StorageProviders.FileSystem;

public sealed class FileSystemStorageProvider : StorageProvider
{
    private readonly FileSystemStorageSettings fileSystemStorageSettings;
    private Stream? outputStream;

    public FileSystemStorageProvider(FileSystemStorageSettings fileSystemStorageSettings)
    {
        this.fileSystemStorageSettings = fileSystemStorageSettings;
    }

    public override async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        string fullPath = CreatePath(path);
        bool exists = await CheckExistsAsync(path, cancellationToken).ConfigureAwait(false);

        if (exists)
        {
            File.Delete(fullPath);
            await DeleteDirectoryAsync(path, cancellationToken).ConfigureAwait(false);
        }
    }

    public override IAsyncEnumerable<string> EnumerateAsync(string? prefix, string[] extensions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        return await CheckExistsAsync(path, cancellationToken).ConfigureAwait(false);
    }

    public override Task<StorageFileInfo?> GetPropertiesAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<Stream?> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        bool exists = await CheckExistsAsync(path, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return null;
        }

        string fullPath = CreatePath(path);
        return File.OpenRead(fullPath);
    }

    public override async Task UploadAsync(string path, Stream stream, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        if (!overwrite)
        {
            bool exists = await CheckExistsAsync(path, cancellationToken).ConfigureAwait(false);
            if (exists)
            {
                throw new IOException($"The file {path} already exists");
            }
        }

        await CreateDirectoryAsync(path, cancellationToken).ConfigureAwait(false);
        string fullPath = CreatePath(path);

        if (stream.CanSeek)
        {
            stream.Position = 0L;
        }

        outputStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (outputStream != null)
            {
                outputStream.Dispose();
                outputStream = null;
            }
        }

        base.Dispose(disposing);
    }

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