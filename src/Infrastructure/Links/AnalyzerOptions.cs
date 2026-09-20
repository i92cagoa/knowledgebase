namespace KnowledgeBase.Infrastructure.Links;

public sealed class AnalyzerOptions
{
    public const string SectionName = "Analyzer";

    /// <summary>"Rules" (default, offline) or "OpenAi"</summary>
    public string Provider { get; set; } = "Rules";
}

public sealed class OpenAiAnalyzerOptions
{
    public const string SectionName = "Analyzer:OpenAi";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
}