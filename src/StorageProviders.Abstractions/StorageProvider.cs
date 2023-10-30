using Microsoft.Extensions.Logging;

namespace StorageProviders;

public abstract class StorageProvider : IStorageProvider
{
    private MemoryStream? memoryStream;
    private bool disposed = false;

    protected StorageProvider(ILogger logger)
    {
        Logger = logger;
    }

    public ILogger Logger { get; }

    public abstract Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    public abstract IAsyncEnumerable<string> EnumerateAsync(string? prefix, string[] extensions, CancellationToken cancellationToken = default);

    public IAsyncEnumerable<string> EnumerateAsync(string? prefix = null, params string[] extensions)
        => EnumerateAsync(prefix, extensions, CancellationToken.None);

    public abstract Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    public abstract Task<StorageFileInfo?> GetPropertiesAsync(string path, CancellationToken cancellationToken = default);

    public abstract Task<Stream?> ReadAsync(string path, CancellationToken cancellationToken = default);

    public async Task<byte[]?> ReadAsByteArrayAsync(string path, CancellationToken cancellationToken = default)
    {
        Stream? stream = await ReadAsync(path, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        stream.Position = 0L;
        memoryStream = new MemoryStream();

        await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
        await stream.DisposeAsync().ConfigureAwait(false);

        byte[] content = memoryStream.ToArray();
        await memoryStream.DisposeAsync().ConfigureAwait(false);

        return content;
    }

    public async Task<string?> ReadAsStringAsync(string path, CancellationToken cancellationToken = default)
    {
        Stream? stream = await ReadAsync(path, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        stream.Position = 0L;
        var reader = new StreamReader(stream);

        string content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await stream.DisposeAsync().ConfigureAwait(false);

        reader.Dispose();
        return content;
    }

    public abstract Task UploadAsync(string path, Stream stream, bool overwrite = false, CancellationToken cancellationToken = default);

    public async Task UploadAsync(string path, byte[] content, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        memoryStream = new MemoryStream(content);
        await UploadAsync(path, memoryStream, overwrite, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            if (memoryStream != null)
            {
                memoryStream.Dispose();
                memoryStream = null;
            }

            disposed = true;
        }
    }

    protected void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}