using AwesomeAssertions;
using KnowledgeBase.Application.Features.Attachments;
using KnowledgeBase.Application.Features.Notes;
using KnowledgeBase.Application.Features.Workspaces;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.UnitTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KnowledgeBase.UnitTests.Features.Attachments;

public sealed class AttachmentServiceTests
{
    private static async Task<Guid> CreateNoteAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var workspaces = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        var notes = scope.ServiceProvider.GetRequiredService<INoteService>();

        var workspaceId = (await workspaces.CreateAsync(new CreateWorkspaceCommand("Dev", null), CancellationToken.None)).Value;
        return (await notes.CreateAsync(new CreateNoteCommand(workspaceId, "Note", "body", []), CancellationToken.None)).Value;
    }

    [Fact]
    public async Task Upload_And_Read_Attachment_Content()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAttachmentService>();

        var noteId = await CreateNoteAsync(fixture.Provider);
        await using var content = new MemoryStream("fake png bytes"u8.ToArray());

        var upload = await service.UploadAsync(
            new UploadAttachmentCommand(noteId, "pic.png", "image/png", AttachmentKind.Image, content),
            CancellationToken.None);

        upload.IsSuccess.Should().BeTrue();

        await using var readScope = fixture.Provider.CreateAsyncScope();
        var readService = readScope.ServiceProvider.GetRequiredService<IAttachmentService>();
        var read = await readService.GetContentAsync(upload.Value, CancellationToken.None);

        read.IsSuccess.Should().BeTrue();
        read.Value!.ContentType.Should().Be("image/png");
        using var reader = new StreamReader(read.Value.Content);
        (await reader.ReadToEndAsync()).Should().Be("fake png bytes");
    }

    [Fact]
    public async Task Upload_To_Unknown_Note_Returns_NotFound()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAttachmentService>();

        await using var content = new MemoryStream("x"u8.ToArray());
        var result = await service.UploadAsync(
            new UploadAttachmentCommand(Guid.NewGuid(), "a.png", "image/png", AttachmentKind.Image, content),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NotFound");
    }

    [Fact]
    public async Task Delete_Attachment_Removes_Content()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAttachmentService>();

        var noteId = await CreateNoteAsync(fixture.Provider);
        await using var content = new MemoryStream("data"u8.ToArray());
        var id = (await service.UploadAsync(
            new UploadAttachmentCommand(noteId, "a.png", "image/png", AttachmentKind.Image, content),
            CancellationToken.None)).Value;

        var result = await service.DeleteAsync(new DeleteAttachmentCommand(id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var after = await service.GetContentAsync(id, CancellationToken.None);
        after.IsFailure.Should().BeTrue();
    }
}