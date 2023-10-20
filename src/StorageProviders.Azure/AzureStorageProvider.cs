using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MimeMapping;
using StorageProviders.Abstractions;

namespace StorageProviders.Azure;

internal class AzureStorageProvider : IStorageProvider
{
    private readonly AzureStorageSettings azureStorageSettings;
    private readonly BlobServiceClient blobServiceClient;
    private readonly IStorageCache cache;

    public AzureStorageProvider(AzureStorageSettings azureStorageSettings, IStorageCache cache)
    {
        blobServiceClient = new BlobServiceClient(azureStorageSettings.ConnectionString);

        this.azureStorageSettings = azureStorageSettings;
        this.cache = cache;
    }

    public async Task DeleteAsync(string path)
    {
        (string containerName, string blobName) = ExtractContainerBlobName(path);
        BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await blobContainerClient.DeleteBlobIfExistsAsync(blobName).ConfigureAwait(false);
        await cache.DeleteAsync(path).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(string path)
    {
        BlobClient blobClient = await GetBlobClientAsync(path).ConfigureAwait(false);
        return await blobClient.ExistsAsync().ConfigureAwait(false);
    }

    public async Task<StorageFileInfo?> GetPropertiesAsync(string path)
    {
        BlobClient blobClient = await GetBlobClientAsync(path).ConfigureAwait(false);
        Response<BlobProperties> properties = await blobClient.GetPropertiesAsync().ConfigureAwait(false);

        string? containerName = azureStorageSettings.ContainerName;
        string blobContainerName = blobClient.BlobContainerName;

        string blobName = blobClient.Name;
        string name = !string.IsNullOrWhiteSpace(containerName) ? $"{blobContainerName}/{blobName}" : blobName;

        var storageFileInfo = new StorageFileInfo(name)
        {
            Length = properties.Value.ContentLength,
            CreationDate = properties.Value.CreatedOn,
            LastModifiedDate = properties.Value.LastModified,
            Metadata = properties.Value.Metadata
        };

        return storageFileInfo;
    }

    public async Task<Stream?> ReadAsync(string path)
    {
        Stream? stream = await cache.GetAsync(path).ConfigureAwait(false);
        if (stream is null)
        {
            BlobClient blobClient = await GetBlobClientAsync(path).ConfigureAwait(false);
            bool blobExists = await blobClient.ExistsAsync().ConfigureAwait(false);

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

        BlobClient blobClient = await GetBlobClientAsync(path, true).ConfigureAwait(false);
        if (!overwrite)
        {
            bool blobExists = await blobClient.ExistsAsync().ConfigureAwait(false);
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
        (string containerName, string blobName) = ExtractContainerBlobName(path);
        BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        if (createIfNotExists)
        {
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None).ConfigureAwait(false);
        }

        return blobContainerClient.GetBlobClient(blobName);
    }

    private (string ContainerName, string BlobName) ExtractContainerBlobName(string? path)
    {
        string normalizedPath = path?.Replace(@"\", "/") ?? string.Empty;
        string? containerName = azureStorageSettings.ContainerName;

        if (!string.IsNullOrWhiteSpace(containerName))
        {
            return (containerName, normalizedPath);
        }
        else
        {
            string root = Path.GetPathRoot(normalizedPath) ?? string.Empty;
            string fileName = normalizedPath[root.Length..];

            string[] parts = fileName.Split('/');
            containerName = parts.First().ToLowerInvariant();

            string blobName = string.Join('/', parts.Skip(1));
            return (containerName, blobName);
        }
    }
}