using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace StorageProviders.Azure;

public sealed partial class AzureStorageProvider : StorageProvider
{
    private async Task<BlobContainerClient> GetBlobContainerClientAsync(string containerName, bool createIfNotExists = false, CancellationToken cancellationToken = default)
    {
        BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
        if (createIfNotExists)
        {
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return blobContainerClient;
    }

    private async Task<BlobClient> GetBlobClientAsync(string path, bool createIfNotExists = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string[] properties = ExtractContainerBlobName(path);

        string containerName = properties.ElementAtOrDefault(0) ?? string.Empty;
        string blobName = properties.ElementAtOrDefault(1) ?? string.Empty;

        BlobContainerClient blobContainerClient = await GetBlobContainerClientAsync(containerName, createIfNotExists, cancellationToken).ConfigureAwait(false);
        return blobContainerClient.GetBlobClient(blobName);
    }

    private string[] ExtractContainerBlobName(string? path)
    {
        string normalizedPath = path?.Replace(@"\", "/") ?? string.Empty;
        string? containerName = azureStorageSettings.ContainerName;

        if (!string.IsNullOrWhiteSpace(containerName))
        {
            return new string[2] { containerName, normalizedPath };
        }
        else
        {
            string root = Path.GetPathRoot(normalizedPath) ?? string.Empty;
            string fileName = normalizedPath[root.Length..];

            string[] parts = fileName.Split('/');
            containerName = parts.First().ToLowerInvariant();

            string blobName = string.Join('/', parts.Skip(1));
            return new string[2] { containerName, blobName };
        }
    }
}