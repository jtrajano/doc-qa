using System;
using System.Collections.Generic;
using System.Text;
using DocQa.API.Chunking;   

namespace DocQa.API.Tests.Chunking;

public class TextChunkerTests
{

    private static TextChunker CreateSut(int size = 1000, int overlap = 200)
        => new(new ChunkerOptions { ChunkSize = size, ChunkOverlap = overlap });

    [Fact]
    public void Chunk_NoPages_ReturnEmpty()
    {
        var result = CreateSut().Chunk(Guid.NewGuid(), []);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\n\n \t")]
    public void Chunk_WhitespaceOnlyPages_ReturnsEmpty(string text)
    {
        var pages = new[] { new PageText(1, text) };
        var result = CreateSut().Chunk(Guid.NewGuid(), pages);
        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_LongText_ReturnsMultipleChunksWithinSizeLimit(){

        var text = string.Join(" ", Enumerable.Repeat("lorem", 100));
        var pages = new[] { new PageText(1, text)};
        
        var result = CreateSut(size: 100, overlap: 20).Chunk(Guid.NewGuid(), pages);
        var expected = Enumerable.Range(0, result.Count).ToArray();
        var actual = result.Select(c=> c.Index).ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Chunk_LongText_ConsecutiveChunksOverlapByConfiguredAmount()
    {
        // "000 001 002 ..." so every region of the text is unique
        var text = string.Concat(Enumerable.Range(0, 200).Select(i => $"{i:000} "));
        var pages = new[] { new PageText(1, text) };

        var result = CreateSut(size: 100, overlap: 20).Chunk(Guid.NewGuid(), pages);

        Assert.True(result.Count > 1);
        for (var i = 0; i < result.Count - 1; i++)
        {
            var firstWordOfNext = result[i + 1].Text.Split(' ')[0];
            Assert.Contains(firstWordOfNext, result[i].Text[^20..]);
        }
    }

    [Fact]
    public void Chunk_LongText_NeverSplitsMidWord()
    {
        // "word000 word001 ..." is 8 chars per word including the space,
        // so a naive cut at 100 lands inside a word
        var text = string.Join(" ", Enumerable.Range(0, 200).Select(i => $"word{i:000}"));
        var pages = new[] { new PageText(1, text) };

        var result = CreateSut(size: 100, overlap: 20).Chunk(Guid.NewGuid(), pages);

        Assert.True(result.Count > 1);
        Assert.All(result, c =>
            Assert.All(c.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                w => Assert.Matches(@"^word\d{3}$", w)));
    }

    [Fact]
    public void Chunk_PrefersParagraphBoundary()
    {
        var p1 = string.Join(" ", Enumerable.Repeat("alpha", 12)); // 71 chars
        var p2 = string.Join(" ", Enumerable.Repeat("bravo", 12)); // 71 chars
        var pages = new[] { new PageText(1, p1 + "\n\n" + p2) };

        var result = CreateSut(size: 100, overlap: 0).Chunk(Guid.NewGuid(), pages);

        Assert.Equal(2, result.Count);
        Assert.Equal(p1, result[0].Text);
        Assert.Equal(p2, result[1].Text);
    }

    [Fact]
    public void Chunk_PrefersSentenceBoundary()
    {
        const string s = "Alpha beta gamma delta epsilon zeta."; // 36 chars
        var pages = new[] { new PageText(1, string.Join(" ", Enumerable.Repeat(s, 4))) };

        var result = CreateSut(size: 100, overlap: 0).Chunk(Guid.NewGuid(), pages);

        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.EndsWith(".", c.Text));
    }

    [Fact]
    public void Chunk_MultiplePages_TracksPageNumbersPerChunk()
    {
        string Words(string prefix, int count) =>
            string.Join(" ", Enumerable.Range(0, count).Select(i => $"{prefix}{i:000}"));

        var pages = new[]
        {
            new PageText(5, Words("a", 8)),   // 39 chars, so the page break lands early in chunk 0
            new PageText(6, Words("b", 30)),
        };

        var result = CreateSut(size: 100, overlap: 20).Chunk(Guid.NewGuid(), pages);

        static int PageOf(string token) => token[0] == 'a' ? 5 : 6;
        static string[] Tokens(string text) =>
            text.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        Assert.True(result.Count > 1);
        Assert.All(result, c =>
        {
            var tokens = Tokens(c.Text);
            Assert.Equal(PageOf(tokens[0]), c.StartPage);
            Assert.Equal(PageOf(tokens[^1]), c.EndPage);
        });
        Assert.Contains(result, c => c.StartPage != c.EndPage); // at least one chunk spans the break
    }

    [Theory]
    [InlineData(0, 0)]      // size must be positive
    [InlineData(-1, 0)]
    [InlineData(100, -1)]   // overlap can't be negative
    [InlineData(100, 100)]  // overlap must be smaller than size
    [InlineData(100, 150)]
    public void Constructor_InvalidOptions_Throws(int size, int overlap)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSut(size, overlap));
    }

}
