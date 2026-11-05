namespace DocQa.API.Extraction;

public interface ITextExtractor
{
    Task<ExtractionResult> ExtractAsync(Stream pdf, CancellationToken cancellationToken = default);
}
