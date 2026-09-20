using AwesomeAssertions;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KnowledgeBase.IntegrationTests.Persistence;

public sealed class NotePersistenceTests : IAsyncLifetime
{
    private readonly string _dbPath;
    private ServiceProvider _provider = null!;

    public NotePersistenceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"kb-integration-{Guid.NewGuid():N}.db");
    }

    public async Task InitializeAsync()
    {
        var options = new DatabaseOptions
        {
            Provider = DatabaseProvider.Sqlite,
            ConnectionString = $"Data Source={_dbPath}"
        };

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opts => opts.UseDatabaseProvider(options));
        _provider = services.BuildServiceProvider();

        await using var scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task Tags_Are_Shared_Across_Notes()
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var workspace = Workspace.Create("Dev", null);
        var dotnet = Tag.Create("dotnet", "#512BD4");

        var n1 = Note.Create("One", "body", workspace.Id);
        var n2 = Note.Create("Two", "body", workspace.Id);
        n1.NoteTags.Add(NoteTag.Create(n1.Id, dotnet.Id));
        n2.NoteTags.Add(NoteTag.Create(n2.Id, dotnet.Id));
        workspace.Notes.Add(n1);
        workspace.Notes.Add(n2);

        db.Workspaces.Add(workspace);
        db.Tags.Add(dotnet);
        await db.SaveChangesAsync();

        var notesWithDotnet = await db.Notes
            .Where(n => n.NoteTags.Any(nt => nt.Tag.Name == "dotnet"))
            .CountAsync();

        notesWithDotnet.Should().Be(2);
        (await db.Tags.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Deleting_Tag_Keeps_Notes_And_Strips_Link()
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var workspace = Workspace.Create("Dev", null);
        var tag = Tag.Create("temporary", "#000000");
        var note = Note.Create("Keep me", "body", workspace.Id);
        note.NoteTags.Add(NoteTag.Create(note.Id, tag.Id));
        workspace.Notes.Add(note);

        db.Workspaces.Add(workspace);
        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        db.Tags.Remove(tag);
        await db.SaveChangesAsync();

        (await db.Notes.CountAsync(n => n.Id == note.Id)).Should().Be(1);
        (await db.NoteTags.CountAsync(nt => nt.TagId == tag.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Note_With_Attachments_Loads_Both_Collections()
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var workspace = Workspace.Create("Docs", null);
        var note = Note.Create("Diagram", "body", workspace.Id);
        note.Attachments.Add(Attachment.Create("a.png", "image/png", "ab/a--ab.png", AttachmentKind.Image, note.Id));
        note.Attachments.Add(Attachment.Create("canvas.json", "application/json", "ab/b--canvas.json", AttachmentKind.Canvas, note.Id));
        workspace.Notes.Add(note);

        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var loaded = await db.Notes
            .AsNoTracking()
            .Include(n => n.NoteTags)
            .Include(n => n.Attachments)
            .SingleAsync(n => n.Id == note.Id);

        loaded.Attachments.Should().HaveCount(2);
        loaded.Attachments.Select(a => a.Kind).Should().Contain(AttachmentKind.Image).And.Contain(AttachmentKind.Canvas);
    }

    [Fact]
    public async Task Workspace_Tree_Loads_Notes_And_Tag_Names()
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var workspace = Workspace.Create("Arch", null);
        var note = Note.Create("EF", "body", workspace.Id);
        var tag = Tag.Create("database");
        note.NoteTags.Add(NoteTag.Create(note.Id, tag.Id));
        workspace.Notes.Add(note);

        db.Workspaces.Add(workspace);
        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        var tree = await db.Workspaces
            .AsNoTracking()
            .Include(w => w.Notes)
            .ThenInclude(n => n.NoteTags)
            .ThenInclude(nt => nt.Tag)
            .SingleAsync(w => w.Id == workspace.Id);

        tree.Notes.Should().ContainSingle();
        tree.Notes.First().NoteTags.Select(nt => nt.Tag.Name).Should().Contain("database");
    }
}