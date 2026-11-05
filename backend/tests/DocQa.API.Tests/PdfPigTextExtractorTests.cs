using System;
using System.Collections.Generic;
using System.Text;
using DocQa.API;

namespace DocQa.API.Tests;

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
}
