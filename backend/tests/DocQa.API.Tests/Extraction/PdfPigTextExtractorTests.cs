using DocQa.API.Exceptions;
using DocQa.API.Extraction;
namespace DocQa.API.Tests.Extraction;

public class PdfPigTextExtractorTests
{
    private readonly ITextExtractor _sut = new PdfPigTextExtractor();

    [Fact]
    public async Task ExtractAsync_SinglePagePdf_ReturnsPageWithNumberAndText()
    {
        using var pdf = TestPdf.WithPages("Hello world");

        var result = await _sut.ExtractAsync(pdf, CancellationToken.None);

        var page = Assert.Single(result.Pages);
        Assert.Equal(1, page.PageNumber);
        Assert.Contains("Hello world", page.Text);
    }

    [Fact]
    public async Task ExtractAsync_CorruptPdf_ThrowsCorruptPdfException()
    {
        await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "corrupt.pdf"));

        await Assert.ThrowsAsync<CorruptPdfException>(() => _sut.ExtractAsync(stream));
    }

    [Fact]
    public async Task ExtractAsync_PdfWithNoPages_ThrowsEmptyPdfException()
    {
        await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "empty.pdf"));



        await Assert.ThrowsAsync<EmptyPdfException>(() => _sut.ExtractAsync(stream));
    }


    [Fact]
    public async Task ExtractAsync_PasswordProtectedPdf_ThrowsPasswordProtectedPdfException()
    {
        await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "Encrypted.pdf"));

        await Assert.ThrowsAsync<PasswordProtectedPdfException>(() => _sut.ExtractAsync(stream));
    }

    [Fact]
    public async Task ExtractAsync_ImageOnlyPdf_ThrowsUnsupportedPdfException()
    {
        await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "Picture.pdf"));

        await Assert.ThrowsAsync<UnsupportedPdfException>(() => _sut.ExtractAsync(stream));
    }

    [Fact]
    public async Task ExtractAsync_MultiPagePdf_ReturnsEachPageWithCorrectNumberAndText()
    {
        using var pdf = TestPdf.WithPages("First page text", "Second page text", "Third page text");

        var result = await _sut.ExtractAsync(pdf);

        Assert.Equal(3, result.Pages.Count);
        Assert.Equal([1, 2, 3], result.Pages.Select(p => p.PageNumber));
        Assert.Contains("First page", result.Pages[0].Text);
        Assert.Contains("Second page", result.Pages[1].Text);
        Assert.Contains("Third page", result.Pages[2].Text);
    }

    [Fact]
    public void AllExtractionExceptions_DeriveFromPdfExtractionException()
    {
        Assert.IsAssignableFrom<PdfExtractionException>(new EmptyPdfException());
        Assert.IsAssignableFrom<PdfExtractionException>(new CorruptPdfException(new Exception()));
        Assert.IsAssignableFrom<PdfExtractionException>(new PasswordProtectedPdfException(new Exception()));
        Assert.IsAssignableFrom<PdfExtractionException>(new UnsupportedPdfException("reason"));
    }
}
