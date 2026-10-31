

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", async (HealthCheckService healthChecks, CancellationToken ct) =>
{
    var report = await healthChecks.CheckHealthAsync(ct);
    var status = report.Status.ToString();

    return report.Status == HealthStatus.Unhealthy
        ? Results.Text(status, contentType: "text/plain", statusCode: StatusCodes.Status503ServiceUnavailable)
        : Results.Text(status, contentType: "text/plain");
})
.WithName("HealthCheck")
.WithTags("Health")
.Produces<string>(StatusCodes.Status200OK, "text/plain")
.Produces<string>(StatusCodes.Status503ServiceUnavailable, "text/plain");





app.Run();



