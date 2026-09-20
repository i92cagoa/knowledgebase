using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.Features.Links;
using KnowledgeBase.Infrastructure.Links;
using KnowledgeBase.Infrastructure.Persistence;
using KnowledgeBase.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace KnowledgeBase.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? new DatabaseOptions();

        services.AddSingleton(databaseOptions);

        services.AddDbContext<AppDbContext>(
            options => options.UseDatabaseProvider(databaseOptions));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<AttachmentStorageOptions>(
            configuration.GetSection(AttachmentStorageOptions.SectionName));
        services.AddScoped<IAttachmentStorage, DiskAttachmentStorage>();

        services.Configure<AnalyzerOptions>(
            configuration.GetSection(AnalyzerOptions.SectionName));
        services.Configure<OpenAiAnalyzerOptions>(
            configuration.GetSection(OpenAiAnalyzerOptions.SectionName));

        services.AddHttpClient<ILinkContentFetcher, HttpLinkContentFetcher>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("KnowledgeBase/1.0");
        });

        var analyzerProvider = configuration
            .GetSection(AnalyzerOptions.SectionName)
            .Get<AnalyzerOptions>()?.Provider ?? "Rules";

        if (analyzerProvider.Equals("OpenAi", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<ILinkAnalyzer, OpenAiLinkAnalyzer>();
        }
        else
        {
            services.AddScoped<ILinkAnalyzer, RulesLinkAnalyzer>();
        }

        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSource("KnowledgeBase.Infrastructure"))
            .WithMetrics(metrics => metrics
                .AddMeter("KnowledgeBase.Infrastructure"));

        return services;
    }
}