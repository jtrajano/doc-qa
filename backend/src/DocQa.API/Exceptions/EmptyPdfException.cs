namespace DocQa.API.Exceptions;

public sealed class EmptyPdfException()
    : PdfExtractionException("The PDF contains no pages.");
