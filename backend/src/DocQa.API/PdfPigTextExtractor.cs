using UglyToad.PdfPig;


namespace DocQa.API;
public sealed class PdfPigTextExtractor : ITextExtractor
{
    public Task<ExtractionResult> ExtractAsync(Stream pdf, CancellationToken cancellationToken = default)
    {
        using var document = PdfDocument.Open(pdf);

        var pages = document.GetPages()
            .Select(p => new PageText(p.Number, p.Text))
            .ToList();

        return Task.FromResult(new ExtractionResult(pages));
    }
}