namespace StorageProviders.Extensions;

public static class StorageProviderExtensions
{
    public static async Task<string?> ReadAsStringAsync(this IStorageProvider storageProvider, string path)
    {
        using var stream = await storageProvider.ReadAsync(path).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        stream.Position = 0;
        using var reader = new StreamReader(stream);

        var content = await reader.ReadToEndAsync().ConfigureAwait(false);
        return content;
    }

    public static async Task<byte[]?> ReadAsByteArrayAsync(this IStorageProvider storageProvider, string path)
    {
        using var stream = await storageProvider.ReadAsync(path).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        stream.Position = 0;
        using var memoryStream = new MemoryStream();

        await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
        stream.Close();

        var content = memoryStream.ToArray();
        return content;
    }

    public static async Task UploadAsync(this IStorageProvider storageProvider, string path, byte[] content, bool overwrite = false)
    {
        using var stream = new MemoryStream(content);
        await storageProvider.UploadAsync(path, stream, overwrite).ConfigureAwait(false);
    }
}