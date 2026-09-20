using AwesomeAssertions;
using KnowledgeBase.Application.Features.Notes;
using KnowledgeBase.Application.Features.Tags;
using KnowledgeBase.Application.Features.Workspaces;
using Xunit;

namespace KnowledgeBase.UnitTests.Validators;

public sealed class WorkspaceValidatorsTests
{
    [Fact]
    public async Task Create_Workspace_Requires_Name()
    {
        var validator = new CreateWorkspaceCommandValidator();

        var invalid = await validator.ValidateAsync(new CreateWorkspaceCommand("", null));
        invalid.IsValid.Should().BeFalse();

        var valid = await validator.ValidateAsync(new CreateWorkspaceCommand("Architecture", null));
        valid.IsValid.Should().BeTrue();
    }
}

public sealed class NoteValidatorsTests
{
    [Fact]
    public async Task Create_Note_Requires_Workspace_Title_And_Content()
    {
        var validator = new CreateNoteCommandValidator();

        var valid = await validator.ValidateAsync(
            new CreateNoteCommand(Guid.NewGuid(), "Title", "body", ["dotnet"]));

        valid.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Create_Note_Empty_Title_Is_Invalid()
    {
        var validator = new CreateNoteCommandValidator();

        var invalid = await validator.ValidateAsync(
            new CreateNoteCommand(Guid.NewGuid(), "", "body", []));

        invalid.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Create_Note_Invalid_SourceUrl_Is_Invalid()
    {
        var validator = new CreateNoteCommandValidator();

        var invalid = await validator.ValidateAsync(
            new CreateNoteCommand(Guid.NewGuid(), "t", "b", [], SourceUrl: "not a url"));

        invalid.IsValid.Should().BeFalse();

        var valid = await validator.ValidateAsync(
            new CreateNoteCommand(Guid.NewGuid(), "t", "b", [], SourceUrl: "https://example.com"));

        valid.IsValid.Should().BeTrue();
    }
}

public sealed class TagValidatorsTests
{
    [Fact]
    public async Task Tag_Requires_Name_And_Hex_Color()
    {
        var validator = new CreateTagCommandValidator();

        (await validator.ValidateAsync(new CreateTagCommand("", "#fff"))).IsValid.Should().BeFalse();
        (await validator.ValidateAsync(new CreateTagCommand("dotnet", "red"))).IsValid.Should().BeFalse();
        (await validator.ValidateAsync(new CreateTagCommand("dotnet", "#512BD4"))).IsValid.Should().BeTrue();
    }
}