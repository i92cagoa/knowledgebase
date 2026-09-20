using KnowledgeBase.Application.Common;
using KnowledgeBase.Infrastructure.Persistence;
using KnowledgeBase.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSource("KnowledgeBase.Infrastructure"))
            .WithMetrics(metrics => metrics
                .AddMeter("KnowledgeBase.Infrastructure"));

        return services;
    }
}