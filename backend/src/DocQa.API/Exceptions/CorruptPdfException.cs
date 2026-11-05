using UglyToad.PdfPig.Core;

namespace DocQa.API.Exceptions;

public class CorruptPdfException:Exception
{
    public CorruptPdfException()
    {
        
    }

    public CorruptPdfException(PdfDocumentFormatException ex)
    {
        
    }
}