using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KnowledgeBase.Application.Features.Links;

namespace KnowledgeBase.Infrastructure.Links;

/// <summary>
/// LLM-backed analyzer that calls an OpenAI-compatible chat-completions endpoint.
/// Activate by setting Storage "[Analyzer] Provider = OpenAi" and providing ApiKey/Model.
/// </summary>
public sealed class OpenAiLinkAnalyzer(HttpClient http, OpenAiAnalyzerOptions options) : ILinkAnalyzer
{
    public async Task<LinkAnalysis> AnalyzeAsync(FetchedLink link, CancellationToken cancellationToken)
    {
        var request = new OpenAiChatRequest(
            options.Model,
            [
                new OpenAiMessage("system", "You extract a short title, a one-sentence summary, and up to 5 lowercase topic tags (single words) from a web article. Reply as JSON: {\"title\": string, \"summary\": string, \"tags\": string[]}."),
                new OpenAiMessage("user", $"Page: {link.Title}\n\n{Truncate(link.Content, 8000)}")
            ]);

        using var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{options.BaseUrl}/chat/completions")
        {
            Content = content
        };
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);

        var response = await http.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<OpenAiChatResponse>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Empty LLM response.");

        var text = result.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        var jsonText = ExtractJsonObject(text);
        var analysis = JsonSerializer.Deserialize<OpenAiLinkAnalysisPayload>(jsonText, JsonOptions);

        var tags = analysis?.Tags ?? [];
        return new LinkAnalysis(
            string.IsNullOrWhiteSpace(analysis?.Title) ? link.Title : analysis.Title,
            string.IsNullOrWhiteSpace(analysis?.Summary) ? (link.Excerpt ?? string.Empty) : analysis.Summary,
            tags);
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max];

    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : "{}";
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed record OpenAiLinkAnalysisPayload(string Title, string Summary, List<string> Tags);
    private sealed record OpenAiChatRequest(string Model, List<OpenAiMessage> Messages);
    private sealed record OpenAiMessage(string Role, string Content);
    private sealed class OpenAiChatResponse
    {
        public List<OpenAiChoice>? Choices { get; set; }
    }

    private sealed class OpenAiChoice
    {
        public OpenAiMessage? Message { get; set; }
    }
}