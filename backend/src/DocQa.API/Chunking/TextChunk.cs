namespace DocQa.API.Chunking;

public sealed record TextChunk(
    Guid documentId,
        int Index,
        string Text,
        int StartPage,
        int EndPage);
