using System.Runtime.CompilerServices;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MimeMapping;

namespace StorageProviders.Azure;

public sealed partial class AzureStorageProvider : StorageProvider
{
    private readonly AzureStorageSettings azureStorageSettings;
    private BlobServiceClient blobServiceClient;

    public AzureStorageProvider(AzureStorageSettings azureStorageSettings)
    {
        blobServiceClient = new BlobServiceClient(azureStorageSettings.ConnectionString);

        this.azureStorageSettings = azureStorageSettings;
    }

    public override async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        string[] properties = ExtractContainerBlobName(path);
        string containerName = properties.ElementAtOrDefault(0) ?? string.Empty;
        string blobName = properties.ElementAtOrDefault(1) ?? string.Empty;

        BlobContainerClient blobContainerClient = await GetBlobContainerClientAsync(containerName, cancellationToken: cancellationToken).ConfigureAwait(false);
        await blobContainerClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public override async IAsyncEnumerable<string> EnumerateAsync(string? prefix, string[] extensions, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        string[] properties = ExtractContainerBlobName(prefix);
        string containerName = properties.ElementAtOrDefault(0) ?? string.Empty;
        string pathPrefix = properties.ElementAtOrDefault(1) ?? string.Empty;

        BlobContainerClient blobContainerClient = await GetBlobContainerClientAsync(containerName, cancellationToken: cancellationToken).ConfigureAwait(false);
        var blobs = blobContainerClient.GetBlobsAsync(prefix: pathPrefix, cancellationToken: cancellationToken).AsPages().WithCancellation(cancellationToken).ConfigureAwait(false);

        await foreach (Page<BlobItem> blobPage in blobs)
        {
            foreach (BlobItem blob in blobPage.Values.Where(b => !b.Deleted &&
                ((!extensions?.Any() ?? true) || extensions!.Any(e => string.Equals(Path.GetExtension(b.Name), e, StringComparison.InvariantCultureIgnoreCase)))))
            {
                string name = string.IsNullOrWhiteSpace(azureStorageSettings.ContainerName) ? $"{containerName}/{blob.Name}" : blob.Name;
                yield return name;
            }
        }
    }

    public override async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        BlobClient blobClient = await GetBlobClientAsync(path, cancellationToken: cancellationToken).ConfigureAwait(false);
        return await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task<StorageFileInfo?> GetPropertiesAsync(string path, CancellationToken cancellationToken = default)
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

    public override async Task<Stream?> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        BlobClient blobClient = await GetBlobClientAsync(path, cancellationToken: cancellationToken).ConfigureAwait(false);
        bool blobExists = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);

        if (!blobExists)
        {
            return null;
        }

        Stream stream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (stream.Position != 0)
        {
            stream.Position = 0L;
        }

        return stream;
    }

    public override async Task UploadAsync(string path, Stream stream, bool overwrite = false, CancellationToken cancellationToken = default)
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
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            blobServiceClient = null!;
        }

        base.Dispose(disposing);
    }
}