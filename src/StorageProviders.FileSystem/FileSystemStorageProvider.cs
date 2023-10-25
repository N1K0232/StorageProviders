namespace StorageProviders.FileSystem;

public sealed partial class FileSystemStorageProvider : StorageProvider
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

        if (stream.CanSeek)
        {
            stream.Position = 0L;
        }

        await CreateDirectoryAsync(path, cancellationToken).ConfigureAwait(false);
        string fullPath = CreatePath(path);

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
}