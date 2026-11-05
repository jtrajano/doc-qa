using UglyToad.PdfPig.Exceptions;

namespace DocQa.API.Exceptions;

public class PasswordProtectedPdfException : Exception
{
    public PasswordProtectedPdfException(PdfDocumentEncryptedException ex)
    {
        
    }
}