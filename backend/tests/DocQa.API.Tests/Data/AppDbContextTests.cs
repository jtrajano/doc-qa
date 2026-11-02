using DocQa.API.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DocQa.API.Tests.Data;

public sealed class AppDbContextTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private DbContextOptions<AppDbContext> _options = default!;

    public async Task InitializeAsync()
    {
        // The in-memory database lives as long as this connection stays open
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var db = new AppDbContext(_options);
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    private static Document NewDocument() => new()
    {
        FileName = "report.pdf",
        ContentType = "application/pdf",
        SizeBytes = 1234,
        BlobPath = "documents/report.pdf"
    };

    [Fact]
    public async Task Saves_and_reads_a_document_with_defaults()
    {
        await using (var db = new AppDbContext(_options))
        {
            db.Documents.Add(NewDocument());
            await db.SaveChangesAsync();
        }

        await using (var db = new AppDbContext(_options))
        {
            var doc = await db.Documents.SingleAsync();

            Assert.NotEqual(Guid.Empty, doc.Id);
            Assert.Equal("report.pdf", doc.FileName);
            Assert.Equal(DocumentStatus.Uploaded, doc.Status);
            Assert.NotEqual(default, doc.CreatedAt);
            Assert.Equal(doc.CreatedAt, doc.UpdatedAt);
        }
    }

    [Fact]
    public async Task Updating_a_document_changes_status_and_bumps_UpdatedAt()
    {
        Guid id;
        await using (var db = new AppDbContext(_options))
        {
            var doc = NewDocument();
            db.Documents.Add(doc);
            await db.SaveChangesAsync();
            id = doc.Id;
        }

        await using (var db = new AppDbContext(_options))
        {
            var doc = await db.Documents.SingleAsync(d => d.Id == id);
            var createdAt = doc.CreatedAt;
            var firstUpdatedAt = doc.UpdatedAt;

            await Task.Delay(20); // make the timestamp difference measurable
            doc.Status = DocumentStatus.Failed;
            doc.ErrorMessage = "Corrupt PDF";
            await db.SaveChangesAsync();

            Assert.Equal(createdAt, doc.CreatedAt);
            Assert.True(doc.UpdatedAt > firstUpdatedAt);
        }

        await using (var db = new AppDbContext(_options))
        {
            var doc = await db.Documents.SingleAsync(d => d.Id == id);
            Assert.Equal(DocumentStatus.Failed, doc.Status);
            Assert.Equal("Corrupt PDF", doc.ErrorMessage);
        }
    }
}