using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MimeMapping;
using StorageProviders.Abstractions;

namespace StorageProviders.Azure;

public sealed class AzureStorageProvider : IStorageProvider
{
    private readonly AzureStorageSettings azureStorageSettings;
    private readonly IStorageCache storageCache;

    private BlobServiceClient blobServiceClient;
    private bool disposed = false;

    public AzureStorageProvider(AzureStorageSettings azureStorageSettings, IStorageCache storageCache)
    {
        blobServiceClient = new BlobServiceClient(azureStorageSettings.ConnectionString);

        this.azureStorageSettings = azureStorageSettings;
        this.storageCache = storageCache;
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        (string containerName, string blobName) = ExtractContainerBlobName(path);
        BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await blobContainerClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken).ConfigureAwait(false);
        await storageCache.DeleteAsync(path, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        BlobClient blobClient = await GetBlobClientAsync(path, cancellationToken: cancellationToken).ConfigureAwait(false);
        return await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<StorageFileInfo?> GetPropertiesAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        BlobClient blobClient = await GetBlobClientAsync(path, cancellationToken: cancellationToken).ConfigureAwait(false);
        Response<BlobProperties> properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

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

    public async Task<Stream?> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        Stream? stream = await storageCache.ReadAsync(path, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            BlobClient blobClient = await GetBlobClientAsync(path, cancellationToken: cancellationToken).ConfigureAwait(false);
            bool blobExists = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);

            if (!blobExists)
            {
                return null;
            }

            stream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        stream.Position = 0;
        return stream;
    }

    public async Task UploadAsync(string path, Stream stream, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        BlobClient blobClient = await GetBlobClientAsync(path, true, cancellationToken).ConfigureAwait(false);
        if (!overwrite)
        {
            bool blobExists = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
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

        await blobClient.UploadAsync(stream, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
        await storageCache.SetAsync(path, stream, TimeSpan.FromHours(1), cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            blobServiceClient = null!;
            disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }

    private async Task<BlobClient> GetBlobClientAsync(string path, bool createIfNotExists = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        (string containerName, string blobName) = ExtractContainerBlobName(path);
        BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        if (createIfNotExists)
        {
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken).ConfigureAwait(false);
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