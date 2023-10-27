using Microsoft.EntityFrameworkCore;
using MimeMapping;
using StorageProviders;
using StorageProvidersSample.BusinessLayer.Models;
using StorageProvidersSample.DataAccessLayer;
using StorageProvidersSample.DataAccessLayer.Entities;

namespace StorageProvidersSample.BusinessLayer;

public class PhotoService : IPhotoService
{
    private readonly DataContext dataContext;
    private readonly IStorageProvider storageProvider;

    public PhotoService(DataContext dataContext, IStorageProvider storageProvider)
    {
        this.dataContext = dataContext;
        this.storageProvider = storageProvider;
    }

    public async Task DeleteAsync(Guid id)
    {
        var photo = await dataContext.Photos.FindAsync(id);
        if (photo != null)
        {
            dataContext.Remove(photo);

            await dataContext.SaveChangesAsync();
            await storageProvider.DeleteAsync(photo.FileName);
        }
    }

    public async Task<IEnumerable<Photo>> GetListAsync()
    {
        var photos = await dataContext.Photos.OrderBy(p => p.FileName)
            .ToListAsync();

        return photos;
    }

    public async Task<StreamFileContent> ReadAsync(Guid id)
    {
        Photo photo = await dataContext.Photos.FindAsync(id);

        if (photo != null)
        {
            Stream stream = await storageProvider.ReadAsync(photo.FileName);

            if (stream != null)
            {
                string contentType = MimeUtility.GetMimeMapping(photo.FileName);

                var content = new StreamFileContent(stream, contentType);
                return content;
            }
        }

        return null;
    }

    public async Task SaveAsync(Stream stream, string fileName, string description)
    {
        var photo = new Photo
        {
            FileName = fileName,
            Length = stream.Length,
            Description = description
        };

        dataContext.Photos.Add(photo);

        await dataContext.SaveChangesAsync();
        await storageProvider.UploadAsync(fileName, stream);
    }
}