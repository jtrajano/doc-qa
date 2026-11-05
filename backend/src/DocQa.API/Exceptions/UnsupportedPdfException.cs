using UglyToad.PdfPig.Core;

namespace DocQa.API.Exceptions;

public class UnsupportedPdfException(string message) : Exception(message)
{
    
}
