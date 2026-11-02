namespace DocQa.API.Storage;

public interface IBlobStorage
{
    Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string blobName, CancellationToken ct = default);
    Task<bool> DeleteAsync(string blobName, CancellationToken ct = default); 
}
