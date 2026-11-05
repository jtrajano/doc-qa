

using Azure.Storage.Blobs;
using DocQa.API.Data;
using DocQa.API.Extraction;
using DocQa.API.Storage;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));

builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection(BlobStorageOptions.Section));
builder.Services.AddOptions<BlobStorageOptions>()
    .Bind(builder.Configuration.GetSection(BlobStorageOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
{
    var o = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
    return new BlobContainerClient(o.ConnectionString, o.ContainerName);
});
builder.Services.AddSingleton<IBlobStorage, AzureBlobStorage>();
builder.Services.AddHostedService<BlobContainerInitializer>();
builder.Services.AddScoped<ITextExtractor,PdfPigTextExtractor>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: ["ready"])
    .AddCheck<BlobStorageHealthCheck>("blob-storage", tags: ["ready"], timeout: TimeSpan.FromSeconds(5));


var app = builder.Build();

// Apply migrations on startup only when explicitly enabled (compose sets this)
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Keep existing /health as-is (liveness), and add a readiness endpoint:
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
}); 

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});



app.Run();
    




