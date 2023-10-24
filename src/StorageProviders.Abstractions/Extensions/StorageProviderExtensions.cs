namespace StorageProviders.Extensions;

public static class StorageProviderExtensions
{
    public static async Task<string?> ReadAsStringAsync(this IStorageProvider storageProvider, string path, CancellationToken cancellationToken = default)
    {
        Stream? stream = await storageProvider.ReadAsync(path, cancellationToken).ConfigureAwait(false);
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

    public static async Task<byte[]?> ReadAsByteArrayAsync(this IStorageProvider storageProvider, string path, CancellationToken cancellationToken = default)
    {
        Stream? stream = await storageProvider.ReadAsync(path, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        stream.Position = 0L;
        var memoryStream = new MemoryStream();

        await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
        await stream.DisposeAsync().ConfigureAwait(false);

        byte[] content = memoryStream.ToArray();
        await memoryStream.DisposeAsync().ConfigureAwait(false);

        return content;
    }

    public static async Task UploadAsync(this IStorageProvider storageProvider, string path, byte[] content, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var stream = new MemoryStream(content);

        await storageProvider.UploadAsync(path, stream, overwrite, cancellationToken).ConfigureAwait(false);
        await stream.DisposeAsync().ConfigureAwait(false);
    }
}