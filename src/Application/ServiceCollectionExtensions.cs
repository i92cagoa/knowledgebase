using System.Reflection;
using FluentValidation;
using KnowledgeBase.Application.Features.Attachments;
using KnowledgeBase.Application.Features.Links;
using KnowledgeBase.Application.Features.Notes;
using KnowledgeBase.Application.Features.Tags;
using KnowledgeBase.Application.Features.Workspaces;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBase.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<ILinkService, LinkService>();

        return services;
    }
}