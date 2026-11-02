namespace DocQa.API.Data;

public enum DocumentStatus { Uploaded, Processing, Ready, Failed }

public class Document
{
    public Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required string BlobPath { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    public string? ErrorMessage { get; set; }   // used by the ingestion pipeline (JEF-22)
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}