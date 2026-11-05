using DocQa.API.Exceptions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Exceptions;

namespace DocQa.API;
public sealed class PdfPigTextExtractor : ITextExtractor
{
    public Task<ExtractionResult> ExtractAsync(Stream pdf, CancellationToken cancellationToken = default)
    {
        if (pdf.CanSeek && pdf.Length == 0)
            throw new EmptyPdfException();

        List<PageText> pages;
        try
        {
            using var document = PdfDocument.Open(pdf);
            pages = ReadPages(document, cancellationToken);
        }
        catch (PdfDocumentEncryptedException ex)
        {
            throw new PasswordProtectedPdfException(ex);
        }
        catch (PdfDocumentFormatException ex)
        {
            throw new CorruptPdfException(ex);
        }
     
        if (pages.Count == 0)
            throw new EmptyPdfException();

        if (pages.All(p => string.IsNullOrWhiteSpace(p.Text)))
            throw new UnsupportedPdfException(
                "The PDF contains no extractable text. Scanned or image-only PDFs are not supported (OCR is out of scope).");

        return Task.FromResult(new ExtractionResult(pages));
        
    }

    private static List<PageText> ReadPages(PdfDocument document, CancellationToken cancellationToken)
    {
        var pages = new List<PageText>();
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            pages.Add(new PageText(page.Number, page.Text));
        }
        return pages;
    }
}