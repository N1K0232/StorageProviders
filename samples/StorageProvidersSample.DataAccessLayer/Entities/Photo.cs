namespace StorageProvidersSample.DataAccessLayer.Entities;

public class Photo
{
    public Guid Id { get; set; }

    public string FileName { get; set; }

    public long Length { get; set; }

    public string Description { get; set; }
}