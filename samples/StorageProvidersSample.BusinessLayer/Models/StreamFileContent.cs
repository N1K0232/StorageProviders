namespace StorageProvidersSample.BusinessLayer.Models;

public class StreamFileContent
{
    public StreamFileContent(Stream stream, string contentType)
    {
        Stream = stream;
        ContentType = contentType;
    }

    public Stream Stream { get; }

    public string ContentType { get; }
}