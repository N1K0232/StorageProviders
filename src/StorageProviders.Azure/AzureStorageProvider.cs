using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MimeMapping;
using StorageProviders.Abstractions;

namespace StorageProviders.Azure;

internal class AzureStorageProvider : IStorageProvider
{
    private readonly AzureStorageSettings settings;
    private readonly BlobServiceClient blobServiceClient;
    private readonly IStorageCache cache;

    public AzureStorageProvider(AzureStorageSettings settings, IStorageCache cache)
    {
        blobServiceClient = new BlobServiceClient(settings.ConnectionString);

        this.settings = settings;
        this.cache = cache;
    }

    public async Task DeleteAsync(string path)
    {
        var (containerName, blobName) = ExtractContainerBlobName(path);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await blobContainerClient.DeleteBlobIfExistsAsync(blobName).ConfigureAwait(false);
        await cache.DeleteAsync(path).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(string path)
    {
        var blobClient = await GetBlobClientAsync(path).ConfigureAwait(false);
        return await blobClient.ExistsAsync().ConfigureAwait(false);
    }

    public async Task<StorageFileInfo> GetPropertiesAsync(string path)
    {
        var blobClient = await GetBlobClientAsync(path).ConfigureAwait(false);
        var properties = await blobClient.GetPropertiesAsync().ConfigureAwait(false);

        var fileInfo = new StorageFileInfo(string.IsNullOrWhiteSpace(settings.ContainerName) ? $"{blobClient.BlobContainerName}/{blobClient.Name}" : blobClient.Name)
        {
            Length = properties.Value.ContentLength,
            CreationDate = properties.Value.CreatedOn,
            LastModifiedDate = properties.Value.LastModified,
            Metadata = properties.Value.Metadata
        };

        return fileInfo;
    }

    public async Task<Stream?> ReadAsync(string path)
    {
        var stream = await cache.GetAsync(path).ConfigureAwait(false);
        if (stream is null)
        {
            var blobClient = await GetBlobClientAsync(path).ConfigureAwait(false);
            var blobExists = await blobClient.ExistsAsync().ConfigureAwait(false);

            if (!blobExists)
            {
                return null;
            }

            stream = await blobClient.OpenReadAsync().ConfigureAwait(false);
        }

        stream.Position = 0;
        return stream;
    }

    public async Task UploadAsync(string path, Stream stream, bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        var blobClient = await GetBlobClientAsync(path, true).ConfigureAwait(false);
        if (!overwrite)
        {
            var blobExists = await blobClient.ExistsAsync().ConfigureAwait(false);
            if (blobExists)
            {
                throw new IOException($"The file {path} already exists");
            }
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        var headers = new BlobHttpHeaders
        {
            ContentType = MimeUtility.GetMimeMapping(path)
        };

        await blobClient.UploadAsync(stream, headers).ConfigureAwait(false);
        await cache.SetAsync(path, stream, TimeSpan.FromHours(1)).ConfigureAwait(false);
    }

    private async Task<BlobClient> GetBlobClientAsync(string path, bool createIfNotExists = false)
    {
        var (containerName, blobName) = ExtractContainerBlobName(path);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        if (createIfNotExists)
        {
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None).ConfigureAwait(false);
        }

        var blobClient = blobContainerClient.GetBlobClient(blobName);
        return blobClient;
    }

    private (string ContainerName, string BlobName) ExtractContainerBlobName(string? path)
    {
        var normalizedPath = path?.Replace(@"\", "/") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(settings.ContainerName))
        {
            return (settings.ContainerName, normalizedPath);
        }

        var root = Path.GetPathRoot(normalizedPath);
        var fileName = normalizedPath[(root ?? string.Empty).Length..];

        var parts = fileName.Split('/');
        var containerName = parts.First().ToLowerInvariant();

        var blobName = string.Join('/', parts.Skip(1));
        return (containerName, blobName);
    }
}