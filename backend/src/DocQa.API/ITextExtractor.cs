namespace DocQa.API;

public interface ITextExtractor
{
    Task<ExtractionResult> ExtractAsync(Stream pdf, CancellationToken cancellationToken = default);
}

public sealed record PageText(int PageNumber, string Text);

public sealed record ExtractionResult(IReadOnlyList<PageText> Pages);