using StorageProvidersSample.BusinessLayer.Models;
using StorageProvidersSample.DataAccessLayer.Entities;

namespace StorageProvidersSample.BusinessLayer;

public interface IPhotoService
{
    Task DeleteAsync(Guid id);

    Task<IEnumerable<Photo>> GetListAsync();

    Task<StreamFileContent> ReadAsync(Guid id);

    Task SaveAsync(Stream stream, string fileName, string description);
}