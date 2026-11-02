using Azure.Storage.Blobs;

namespace DocQa.API.Storage;

public sealed class BlobContainerInitializer(
    BlobContainerClient container,
    ILogger<BlobContainerInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            await container.CreateIfNotExistsAsync(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not ensure blob container at startup; /health/ready reports storage state.");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}