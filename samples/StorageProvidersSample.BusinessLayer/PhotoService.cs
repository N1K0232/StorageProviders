using MimeMapping;
using StorageProviders;
using StorageProvidersSample.BusinessLayer.Models;

namespace StorageProvidersSample.BusinessLayer;

public class PhotoService : IPhotoService
{
    private readonly IStorageProvider storageProvider;

    public PhotoService(IStorageProvider storageProvider)
    {
        this.storageProvider = storageProvider;
    }

    public async Task DeleteAsync(string fileName)
    {
        await storageProvider.DeleteAsync(fileName);
    }

    public async Task<StreamFileContent> ReadAsync(string fileName)
    {
        Stream stream = await storageProvider.ReadAsync(fileName);
        if (stream != null)
        {
            string contentType = MimeUtility.GetMimeMapping(fileName);

            var content = new StreamFileContent(stream, contentType);
            return content;
        }

        return null;
    }

    public async Task SaveAsync(string fileName, Stream stream)
    {
        await storageProvider.UploadAsync(fileName, stream);
    }
}