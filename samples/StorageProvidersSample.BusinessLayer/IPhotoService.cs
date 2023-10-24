using StorageProvidersSample.BusinessLayer.Models;

namespace StorageProvidersSample.BusinessLayer;

public interface IPhotoService
{
    Task DeleteAsync(string fileName);

    Task<StreamFileContent> ReadAsync(string fileName);

    Task SaveAsync(string fileName, Stream stream);
}