using DocQa.API.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DocQa.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Satisfies options validation. Nothing connects to it, because the initializer is removed below.
        builder.UseSetting("BlobStorage:ConnectionString", "UseDevelopmentStorage=true");
        builder.UseSetting("BlobStorage:ContainerName", "test-documents");

        builder.ConfigureServices(services =>
        {
            var initializer = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType == typeof(BlobContainerInitializer));

            if (initializer is not null) services.Remove(initializer);
        });
    }
}