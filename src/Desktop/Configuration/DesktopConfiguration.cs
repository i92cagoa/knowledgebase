using Microsoft.Extensions.Configuration;

namespace KnowledgeBase.Desktop.Configuration;

public sealed record ApiOptions
{
    public const string SectionName = "Api";

    public string BaseUrl { get; set; } = "http://localhost:5124";
}

public static class DesktopConfiguration
{
    public static ApiOptions LoadApiOptions()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

        var configuration = builder.Build();

        var options = new ApiOptions();
        configuration.GetSection(ApiOptions.SectionName).Bind(options);
        return options;
    }
}