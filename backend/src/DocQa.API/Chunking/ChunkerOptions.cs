namespace DocQa.API.Chunking;

public sealed record ChunkerOptions
{
    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 200;
}
