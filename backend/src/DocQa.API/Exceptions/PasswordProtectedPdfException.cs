namespace DocQa.API.Exceptions;

public sealed class PasswordProtectedPdfException(Exception innerException)
    : PdfExtractionException("The PDF is password protected.", innerException);
