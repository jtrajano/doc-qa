using DocQa.API.Extraction;
using RecursiveTextSplitting;

namespace DocQa.API.Chunking;

public interface IChunker
{
    IReadOnlyList<TextChunk> Chunk(Guid documentId, IReadOnlyList<PageText> pages);

}
