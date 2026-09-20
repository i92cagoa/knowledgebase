using AwesomeAssertions;
using KnowledgeBase.Domain.Entities;
using Xunit;

namespace KnowledgeBase.UnitTests.Entities;

public class WorkspaceTests
{
    [Fact]
    public void Create_Should_Set_All_Properties()
    {
        var workspace = Workspace.Create("Development", "Notes about dev");

        workspace.Id.Should().NotBeEmpty();
        workspace.Name.Should().Be("Development");
        workspace.Description.Should().Be("Notes about dev");
        workspace.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        workspace.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Update_Should_Refresh_UpdatedAt()
    {
        var workspace = Workspace.Create("Development", null);
        workspace.Update("Architecture", "Notes about architecture");

        workspace.Name.Should().Be("Architecture");
        workspace.Description.Should().Be("Notes about architecture");
        workspace.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}