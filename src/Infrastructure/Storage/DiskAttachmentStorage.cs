using KnowledgeBase.Application.Common;
using Microsoft.Extensions.Options;

namespace KnowledgeBase.Infrastructure.Storage;

public sealed class AttachmentStorageOptions
{
    public const string SectionName = "Storage";

    public string RootPath { get; set; } = "uploads";
}

public sealed class DiskAttachmentStorage : IAttachmentStorage
{
    private readonly string _rootPath;

    public DiskAttachmentStorage(IOptions<AttachmentStorageOptions> options)
    {
        _rootPath = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Guid attachmentId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(fileName);
        var relativePath = Path.Combine(attachmentId.ToString("N")[..2], $"{attachmentId:N}--{safeName}");
        var fullPath = Path.Combine(_rootPath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, cancellationToken);

        return relativePath;
    }

    public Task<Stream?> OpenAsync(string storagePath, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(_rootPath, storagePath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(_rootPath, storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}