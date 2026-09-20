using KnowledgeBase.Application.Features.Links;

namespace KnowledgeBase.Infrastructure.Links;

/// <summary>
/// Lightweight offline analyzer: extracts candidate tags from frequent words and uses the
/// page title + excerpt as the summary. Used as a default provider when no external LLM is
/// configured. Swappable via <see cref="AnalyzerOptions.Provider"/>.
/// </summary>
public sealed class RulesLinkAnalyzer : ILinkAnalyzer
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "but", "of", "in", "on", "at", "to", "for", "with",
        "as", "by", "is", "are", "was", "were", "be", "been", "this", "that", "these", "those",
        "it", "its", "from", "up", "about", "into", "over", "not", "no", "if", "then", "than",
        "too", "very", "can", "will", "just", "should", "also", "their", "there", "here",
        "you", "your", "our", "we", "they", "them", "he", "she", "his", "her", "has", "have",
        "had", "do", "does", "did", "what", "when", "where", "which", "who", "whom", "how",
        "use", "using", "used", "new", "more", "most", "some", "any", "all", "each", "every"
    };

    private static readonly int[] TagLengthMinMax = [3, 24];

    public Task<LinkAnalysis> AnalyzeAsync(FetchedLink link, CancellationToken cancellationToken)
    {
        var words = Tokenize(link.Content);
        var frequencies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var word in words)
        {
            if (StopWords.Contains(word) || word.Length < TagLengthMinMax[0] || word.Length > TagLengthMinMax[1])
            {
                continue;
            }

            frequencies.TryGetValue(word, out var count);
            frequencies[word] = count + 1;
        }

        var tags = frequencies
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .Select(kv => kv.Key.ToLowerInvariant())
            .ToList();

        var summary = string.IsNullOrWhiteSpace(link.Excerpt)
            ? link.Content.Length <= 400 ? link.Content : link.Content[..400] + "…"
            : link.Excerpt;

        return Task.FromResult(new LinkAnalysis(link.Title, summary, tags));
    }

    private static IEnumerable<string> Tokenize(string content)
    {
        return content
            .Split([' ', '\t', '\r', '\n', ',', ';', '.', ':', '(', ')', '[', ']', '"', '!', '?'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}