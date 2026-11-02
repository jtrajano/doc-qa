
using Testcontainers.Azurite;

namespace DocQa.API.Tests.Storage;

public sealed class AzuriteFixture : IAsyncLifetime
{
    private readonly AzuriteContainer _container = new AzuriteBuilder()
     .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
     .WithCommand("--skipApiVersionCheck")
     .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}