namespace DocQa.API.Exceptions;

public abstract class PdfExtractionException : Exception
{
    protected PdfExtractionException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}