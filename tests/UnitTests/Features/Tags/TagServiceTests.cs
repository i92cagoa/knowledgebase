using AwesomeAssertions;
using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.Features.Tags;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.UnitTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KnowledgeBase.UnitTests.Features.Tags;

public sealed class TagServiceTests
{
    [Fact]
    public async Task Create_Auto_Generates_Id()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();

        var result = await service.CreateAsync(new CreateTagCommand("dotnet", "#512BD4"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var tag = await db.Tags.FindAsync([result.Value]);
        tag!.Name.Should().Be("dotnet");
        tag.Color.Should().Be("#512BD4");
    }

    [Fact]
    public async Task Create_Duplicate_Name_Returns_Conflict()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();

        await service.CreateAsync(new CreateTagCommand("dotnet", "#000000"), CancellationToken.None);
        var result = await service.CreateAsync(new CreateTagCommand("dotnet", "#512BD4"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Conflict");
    }

    [Fact]
    public async Task Create_Invalid_Color_Returns_Invalid()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();

        var result = await service.CreateAsync(new CreateTagCommand("dotnet", "red"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Invalid");
    }

    [Fact]
    public async Task List_Returns_Alphabetical_Tags()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        db.Tags.Add(Tag.Create("zebra"));
        db.Tags.Add(Tag.Create("apple", "#ff0000"));
        await db.SaveChangesAsync();

        var result = await service.ListAsync(CancellationToken.None);

        result.Value!.Select(t => t.Name).Should().Equal("apple", "zebra");
    }

    [Fact]
    public async Task Delete_Unknown_Returns_NotFound()
    {
        using var fixture = new TestAppDbContext();
        await using var scope = fixture.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITagService>();

        var result = await service.DeleteAsync(new DeleteTagCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NotFound");
    }
}