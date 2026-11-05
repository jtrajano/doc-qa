namespace DocQa.API.Exceptions;

public sealed class UnsupportedPdfException(string reason)
    : PdfExtractionException(reason);