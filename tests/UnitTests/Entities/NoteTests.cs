using AwesomeAssertions;
using KnowledgeBase.Domain.Entities;
using Xunit;

namespace KnowledgeBase.UnitTests.Entities;

public class NoteTests
{
    [Fact]
    public void Create_Should_Set_All_Properties()
    {
        var note = Note.Create(
            "Testing in .NET",
            "# Testing\n\nUnit tests with xUnit.",
            Guid.NewGuid());

        note.Id.Should().NotBeEmpty();
        note.Title.Should().Be("Testing in .NET");
        note.ContentMarkdown.Should().Be("# Testing\n\nUnit tests with xUnit.");
        note.SourceUrl.Should().BeNull();
        note.SourceSummary.Should().BeNull();
        note.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        note.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        note.Attachments.Should().BeEmpty();
        note.NoteTags.Should().BeEmpty();
    }

    [Fact]
    public void Update_Should_Replace_Content_And_Refresh_UpdatedAt()
    {
        var note = Note.Create("Title", "old", Guid.NewGuid());
        note.Update("New Title", "# new", "https://example.com", "A summary");

        note.Title.Should().Be("New Title");
        note.ContentMarkdown.Should().Be("# new");
        note.SourceUrl.Should().Be("https://example.com");
        note.SourceSummary.Should().Be("A summary");
        note.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

public class TagTests
{
    [Fact]
    public void Create_Should_Use_Default_Color()
    {
        var tag = Tag.Create("dotnet");

        tag.Name.Should().Be("dotnet");
        tag.Color.Should().BeEmpty();
    }

    [Fact]
    public void Create_Should_Use_Provided_Color()
    {
        var tag = Tag.Create("dotnet", "#512BD4");

        tag.Color.Should().Be("#512BD4");
    }
}

public class AttachmentTests
{
    [Fact]
    public void Create_Should_Set_Properties()
    {
        var attachment = Attachment.Create("diagram.png", "image/png", "/data/9a.png", AttachmentKind.Image, Guid.NewGuid());

        attachment.FileName.Should().Be("diagram.png");
        attachment.ContentType.Should().Be("image/png");
        attachment.StoragePath.Should().Be("/data/9a.png");
        attachment.Kind.Should().Be(AttachmentKind.Image);
    }
}