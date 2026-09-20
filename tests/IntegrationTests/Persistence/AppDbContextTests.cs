using AwesomeAssertions;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KnowledgeBase.IntegrationTests.Persistence;

public sealed class AppDbContextTests : IDisposable
{
    private readonly string _dbPath;
    private readonly AppDbContext _context;

    public AppDbContextTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"knowledgebase-test-{Guid.NewGuid():N}.db");

        var options = new DatabaseOptions
        {
            Provider = DatabaseProvider.Sqlite,
            ConnectionString = $"Data Source={_dbPath}"
        };

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opts => opts.UseDatabaseProvider(options));

        var provider = services.BuildServiceProvider();
        _context = provider.GetRequiredService<AppDbContext>();
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task Workspace_Note_Tags_Relationships_Are_Created()
    {
        var workspace = Workspace.Create("Architecture", "notes");
        var note = Note.Create("EF Core", "# ef core", workspace.Id);
        var tagDotnet = Tag.Create("dotnet");
        var tagDatabase = Tag.Create("database");

        note.NoteTags.Add(NoteTag.Create(note.Id, tagDotnet.Id));
        note.NoteTags.Add(NoteTag.Create(note.Id, tagDatabase.Id));
        workspace.Notes.Add(note);

        _context.Workspaces.Add(workspace);
        _context.Tags.AddRange(tagDotnet, tagDatabase);
        _context.NoteTags.AddRange(note.NoteTags);

        await _context.SaveChangesAsync();

        var loaded = await _context.Workspaces
            .Include(w => w.Notes)
            .ThenInclude(n => n.NoteTags)
            .ThenInclude(nt => nt.Tag)
            .SingleAsync(w => w.Id == workspace.Id);

        loaded.Name.Should().Be("Architecture");
        loaded.Notes.Should().ContainSingle();
        loaded.Notes.First().Title.Should().Be("EF Core");
        loaded.Notes.First().NoteTags.Should().HaveCount(2);
        loaded.Notes.First().NoteTags.Select(nt => nt.Tag.Name).Should().Contain("dotnet").And.Contain("database");
    }

    [Fact]
    public async Task Attachment_Is_Persisted_With_Note()
    {
        var workspace = Workspace.Create("Docs", null);
        var note = Note.Create("Diagram", "## diagram", workspace.Id);
        note.Attachments.Add(Attachment.Create("a.png", "image/png", "/files/a.png", AttachmentKind.Image, note.Id));
        workspace.Notes.Add(note);

        _context.Workspaces.Add(workspace);

        await _context.SaveChangesAsync();

        var loaded = await _context.Notes
            .Include(n => n.Attachments)
            .SingleAsync(n => n.Id == note.Id);

        loaded.Attachments.Should().ContainSingle();
        loaded.Attachments.First().Kind.Should().Be(AttachmentKind.Image);
    }

    [Fact]
    public async Task Delete_Workspace_Cascades_To_Notes()
    {
        var workspace = Workspace.Create("DeleteMe", null);
        var note = Note.Create("To delete", "content", workspace.Id);
        workspace.Notes.Add(note);

        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        _context.Workspaces.Remove(workspace);
        await _context.SaveChangesAsync();

        (await _context.Notes.CountAsync(n => n.Id == note.Id)).Should().Be(0);
        (await _context.NoteTags.CountAsync()).Should().Be(0);
    }
}