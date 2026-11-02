using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;      // BlobUploadOptions, BlobHttpHeaders

namespace DocQa.API.Storage;

public class AzureBlobStorage(BlobContainerClient container) : IBlobStorage
{
    public async Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct = default)
    {
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, ct);
        return blobName;
    }

    public async Task<Stream> DownloadAsync(string blobName, CancellationToken ct = default)
        => await container.GetBlobClient(blobName).OpenReadAsync(cancellationToken: ct);

    public async Task<bool> DeleteAsync(string blobName, CancellationToken ct = default)
        => (await container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct)).Value;
}
