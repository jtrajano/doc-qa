namespace DocQa.API.Exceptions;

public sealed class CorruptPdfException(Exception innerException)
    : PdfExtractionException("The PDF is corrupt or unreadable.", innerException);
