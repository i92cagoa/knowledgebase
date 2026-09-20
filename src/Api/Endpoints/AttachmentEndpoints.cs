using KnowledgeBase.Application.Features.Attachments;
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Api.Endpoints;

public static class AttachmentEndpoints
{
    public static IEndpointRouteBuilder MapAttachmentEndpoints(this IEndpointRouteBuilder app)
    {
        var notes = app.MapGroup("api/notes/{noteId:guid}/attachments")
            .WithTags("Attachments");

        notes.MapPost("/", async (Guid noteId, AttachmentKind kind, IFormFile file, IAttachmentService service, CancellationToken ct) =>
        {
            var fileName = file.FileName;
            var contentType = string.IsNullOrEmpty(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;

            await using var stream = file.OpenReadStream();

            var command = new UploadAttachmentCommand(noteId, fileName, contentType, kind, stream);
            var result = await service.UploadAsync(command, ct);

            return result.IsSuccess
                ? Results.Created($"/api/attachments/{result.Value}", result.Value)
                : result.ToHttpResult();
        })
            .DisableAntiforgery();

        var attachments = app.MapGroup("api/attachments")
            .WithTags("Attachments");

        attachments.MapGet("/{id:guid}", async (Guid id, IAttachmentService service, CancellationToken ct) =>
        {
            var result = await service.GetContentAsync(id, ct);
            if (result.IsFailure)
            {
                return result.ToHttpResult();
            }

            var content = result.Value!;
            return Results.File(content.Content, content.ContentType, content.FileName, enableRangeProcessing: true);
        });

        attachments.MapDelete("/{id:guid}", async (Guid id, IAttachmentService service, CancellationToken ct) =>
            (await service.DeleteAsync(new DeleteAttachmentCommand(id), ct)).ToHttpResult());

        return app;
    }
}