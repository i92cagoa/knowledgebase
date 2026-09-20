using AwesomeAssertions;
using KnowledgeBase.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Xunit;

namespace KnowledgeBase.IntegrationTests.Storage;

public sealed class DiskAttachmentStorageTests : IDisposable
{
    private readonly string _root;
    private readonly DiskAttachmentStorage _storage;

    public DiskAttachmentStorageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"kb-storage-{Guid.NewGuid():N}");
        _storage = new DiskAttachmentStorage(Options.Create(new AttachmentStorageOptions { RootPath = _root }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Fact]
    public async Task Save_Then_Open_Returns_Same_Bytes()
    {
        using var content = new MemoryStream("hello storage"u8.ToArray());

        var relativePath = await _storage.SaveAsync(Guid.NewGuid(), "file.txt", content, CancellationToken.None);

        relativePath.Should().NotBeEmpty();

        var opened = await _storage.OpenAsync(relativePath, CancellationToken.None);
        opened.Should().NotBeNull();
        using (opened!)
        {
            using var reader = new StreamReader(opened);
            (await reader.ReadToEndAsync()).Should().Be("hello storage");
        }
    }

    [Fact]
    public async Task Save_Sanitizes_FileName()
    {
        using var content = new MemoryStream("x"u8.ToArray());

        var relativePath = await _storage.SaveAsync(Guid.NewGuid(), "../../../evil.txt", content, CancellationToken.None);

        relativePath.Should().NotContain("..");
        var fullPath = Path.Combine(_root, relativePath);
        fullPath.Should().StartWith(Path.GetFullPath(_root));
    }

    [Fact]
    public async Task Open_Missing_Returns_Null()
    {
        (await _storage.OpenAsync("nope.png", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_Removes_File()
    {
        using var content = new MemoryStream("x"u8.ToArray());
        var relativePath = await _storage.SaveAsync(Guid.NewGuid(), "a.txt", content, CancellationToken.None);

        await _storage.DeleteAsync(relativePath, CancellationToken.None);

        (await _storage.OpenAsync(relativePath, CancellationToken.None)).Should().BeNull();
    }
}