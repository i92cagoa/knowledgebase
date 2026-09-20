using System.Text;
using System.Text.RegularExpressions;
using KnowledgeBase.Application.Features.Links;

namespace KnowledgeBase.Infrastructure.Links;

public sealed class HttpLinkContentFetcher(HttpClient http) : ILinkContentFetcher
{
    private static readonly Regex HtmlTagsRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public async Task<FetchedLink> FetchAsync(Uri url, CancellationToken cancellationToken)
    {
        var response = await http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var title = ExtractTitle(html) ?? url.ToString();
        var content = StripHtml(html);
        const int excerptLength = 400;

        return new FetchedLink(
            title,
            content,
            content.Length <= excerptLength ? content : content[..excerptLength] + "…");
    }

    private static string? ExtractTitle(string html)
    {
        var match = Regex.Match(html, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
        {
            return null;
        }

        var title = HtmlTagsRegex.Replace(match.Groups[1].Value, string.Empty);
        return WhitespaceRegex.Replace(title, " ").Trim();
    }

    private static string StripHtml(string html)
    {
        // drop script/style blocks entirely, then strip remaining tags
        var noScripts = Regex.Replace(
            html,
            @"<(script|style|nav|footer|header)[^>]*>.*?</\1>",
            " ",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        var text = HtmlTagsRegex.Replace(noScripts, " ");
        text = System.Net.WebUtility.HtmlDecode(text);

        return WhitespaceRegex.Replace(text, " ").Trim();
    }
}