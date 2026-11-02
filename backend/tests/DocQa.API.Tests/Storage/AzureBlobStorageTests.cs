using Azure.Storage.Blobs;
using DocQa.API.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace DocQa.API.Tests.Storage;

[Trait("Category", "Integration")]
public sealed class AzureBlobStorageTests(AzuriteFixture azurite) : IClassFixture<AzuriteFixture>
{
    // Unique container per test so tests can't interfere with each other.
    private BlobContainerClient NewContainer() =>
        new(azurite.ConnectionString, $"test-{Guid.NewGuid():N}");

    [Fact]
    public async Task Upload_then_download_returns_same_content()
    {
        var container = NewContainer();
        await container.CreateIfNotExistsAsync();
        var storage = new AzureBlobStorage(container);

        var bytes = Encoding.UTF8.GetBytes("hello azurite");
        await storage.UploadAsync("docs/hello.txt", new MemoryStream(bytes), "text/plain");

        await using var downloaded = await storage.DownloadAsync("docs/hello.txt");
        using var ms = new MemoryStream();
        await downloaded.CopyToAsync(ms);

        Assert.Equal(bytes, ms.ToArray());
    }

    [Fact]
    public async Task Delete_removes_blob_and_reports_false_when_already_gone()
    {
        var container = NewContainer();
        await container.CreateIfNotExistsAsync();
        var storage = new AzureBlobStorage(container);

        await storage.UploadAsync("a.txt", new MemoryStream([1, 2, 3]), "application/octet-stream");

        Assert.True(await storage.DeleteAsync("a.txt"));
        Assert.False(await storage.DeleteAsync("a.txt"));
    }

    [Fact]
    public async Task Initializer_creates_missing_container()
    {
        var container = NewContainer();
        Assert.False(await container.ExistsAsync());

        await new BlobContainerInitializer(container, NullLogger<BlobContainerInitializer>.Instance)
            .StartAsync(CancellationToken.None);

        Assert.True((await container.ExistsAsync()).Value);
    }

    [Fact]
    public async Task Health_check_is_healthy_when_container_exists_and_unhealthy_when_not()
    {
        var container = NewContainer();
        var check = new BlobStorageHealthCheck(container);

        var missing = await check.CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Unhealthy, missing.Status);

        await container.CreateIfNotExistsAsync();
        var present = await check.CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Healthy, present.Status);
    }
}