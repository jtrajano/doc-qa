using System.Text;

namespace DocQa.API.Chunking;

public sealed class TextChunker : IChunker
{
    private readonly ChunkerOptions _options;

    public TextChunker(ChunkerOptions? options = null)
    {
        _options = options ?? new ChunkerOptions();

        if (_options.ChunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(options),
                $"ChunkSize must be positive, but was {_options.ChunkSize}.");

        if (_options.ChunkOverlap < 0)
            throw new ArgumentOutOfRangeException(nameof(options),
                $"ChunkOverlap cannot be negative, but was {_options.ChunkOverlap}.");

        if (_options.ChunkOverlap >= _options.ChunkSize)
            throw new ArgumentOutOfRangeException(nameof(options),
                $"ChunkOverlap ({_options.ChunkOverlap}) must be smaller than ChunkSize ({_options.ChunkSize}).");
    }

    public IReadOnlyList<TextChunk> Chunk(Guid documentId, IReadOnlyList<PageText> pages)
    {
        var content = pages.Where(p => !string.IsNullOrWhiteSpace(p.Text)).ToList();
        if (content.Count == 0) return [];

        var sb = new StringBuilder();
        var pageStarts = new List<(int Offset, int Page)>();
        foreach (var p in content)
        {
            if (sb.Length > 0) sb.Append("\n\n");
            pageStarts.Add((sb.Length, p.PageNumber));
            sb.Append(p.Text.Trim());
        }
        var text = sb.ToString();

        var chunks = new List<TextChunk>();
        var start = 0;
        while (start < text.Length)
        {
            var end = Math.Min(start + _options.ChunkSize, text.Length);

            if (end < text.Length)
            {
                var cut = FindCutPoint(text, start, end);
                if (cut > start) end = cut;
            }

            // trim, but remember where the real text begins and ends
            var first = start;
            var last = end;
            while (first < last && char.IsWhiteSpace(text[first])) first++;
            while (last > first && char.IsWhiteSpace(text[last - 1])) last--;

            if (last > first)
            {
                chunks.Add(new TextChunk(
                    documentId,
                    chunks.Count,
                    text[first..last],
                    PageAt(pageStarts, first),
                    PageAt(pageStarts, last - 1)));
            }

            if (end >= text.Length) break;

            var next = Math.Max(end - _options.ChunkOverlap, start + 1);
            while (next < end && !char.IsWhiteSpace(text[next - 1])) next++;
            start = next;
        }
        return chunks;
    }

    private static int PageAt(List<(int Offset, int Page)> pageStarts, int position)
    {
        var page = pageStarts[0].Page;
        foreach (var (offset, p) in pageStarts)
        {
            if (offset <= position) page = p;
            else break;
        }
        return page;
    }

    private static int LastWhitespace(string text, int start, int end)
    {
        for (var i = end; i > start; i--)
            if (char.IsWhiteSpace(text[i])) return i;
        return -1;
    }

    private static int FindCutPoint(string text, int start, int end)
    {
        // don't accept a boundary so early that the chunk would be tiny
        var minCut = start + (end - start) / 2;

        var cut = LastParagraphBreak(text, start, end);
        if (cut >= minCut) return cut;

        cut = LastSentenceEnd(text, start, end);
        if (cut >= minCut) return cut;

        return LastWhitespace(text, start, end); // may be -1
    }

    private static int LastParagraphBreak(string text, int start, int end)
    {
        for (var i = end - 1; i >= start; i--)
            if (text[i] == '\n' && text[i + 1] == '\n') return i;
        return -1;
    }

    private static int LastSentenceEnd(string text, int start, int end)
    {
        for (var i = end; i > start; i--)
            if (char.IsWhiteSpace(text[i]) && text[i - 1] is '.' or '!' or '?') return i;
        return -1;
    }
}