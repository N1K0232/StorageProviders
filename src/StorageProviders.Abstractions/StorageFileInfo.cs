using MimeMapping;

namespace StorageProviders;

public sealed class StorageFileInfo
{
    public StorageFileInfo(string name)
    {
        Name = name;
        ContentType = MimeUtility.GetMimeMapping(name);
    }

    public string Name { get; }

    public object ContentType { get; }

    public DateTimeOffset CreationDate { get; set; }

    public DateTimeOffset LastModifiedDate { get; set; }

    public long Length { get; set; }

    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}