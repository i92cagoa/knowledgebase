using System.Net.Http.Json;
using KnowledgeBase.Desktop.Models;

namespace KnowledgeBase.Desktop.Services;

public sealed class KnowledgeBaseApiClient
{
    private readonly HttpClient _http;

    public KnowledgeBaseApiClient(string baseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public Task<List<WorkspaceTree>?> GetTreeAsync() =>
        _http.GetFromJsonAsync<List<WorkspaceTree>>("api/workspaces?embed=notes");

    public Task<List<Tag>?> GetTagsAsync() =>
        _http.GetFromJsonAsync<List<Tag>>("api/tags");

    public Task<Note?> GetNoteAsync(Guid id) =>
        _http.GetFromJsonAsync<Note>($"api/notes/{id}");

    public Task<List<Workspace>?> GetWorkspacesAsync() =>
        _http.GetFromJsonAsync<List<Workspace>>("api/workspaces");

    public async Task<Guid> CreateWorkspaceAsync(string name, string? description)
    {
        var response = await _http.PostAsJsonAsync("api/workspaces", new CreateWorkspaceRequest(name, description));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task DeleteWorkspaceAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/workspaces/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<Guid> CreateNoteAsync(Guid workspaceId, CreateNoteRequest request)
    {
        var response = await _http.PostAsJsonAsync($"api/workspaces/{workspaceId}/notes", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task UpdateNoteAsync(Guid id, UpdateNoteRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/notes/{id}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteNoteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/notes/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<Guid> UploadAttachmentAsync(Guid noteId, string filePath, bool isCanvas)
    {
        var kind = isCanvas ? 2 : 1;
        await using var file = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(file);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GuessContentType(filePath));
        content.Add(streamContent, "file", Path.GetFileName(filePath));

        var response = await _http.PostAsync($"api/notes/{noteId}/attachments?kind={kind}", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    public async Task DeleteAttachmentAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/attachments/{id}");
        response.EnsureSuccessStatusCode();
    }

    private static string GuessContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
    }
}