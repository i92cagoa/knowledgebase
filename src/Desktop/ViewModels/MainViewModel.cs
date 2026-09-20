using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KnowledgeBase.Desktop.Models;
using KnowledgeBase.Desktop.Services;

namespace KnowledgeBase.Desktop.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly IKnowledgeBaseApiClient _api;
    private Guid? _editingNoteId;
    private Guid? _currentWorkspaceId;
    private WorkspaceNode? _selectedWorkspace;

    public MainViewModel(IKnowledgeBaseApiClient api)
    {
        _api = api;
        Tree = new TreeViewModel(api);
        Tree.NoteSelectionChanged += OnNoteSelected;
        Tree.WorkspaceSelectionChanged += OnWorkspaceSelected;
        Search = new SearchViewModel(api, OnSearchItemSelected);
        Tags = new TagManagerViewModel(api);
        Graph = new GraphViewModel(api);
        Graph.NodeSelected += OnGraphNodeSelected;
        ImportLink = new ImportLinkViewModel(api, () => _currentWorkspaceId, () => _ = LoadAllAsync());
        NewWorkspace = new NewWorkspaceViewModel(api, () => _ = LoadAllAsync());
    }

    public TreeViewModel Tree { get; }

    public SearchViewModel Search { get; }

    public TagManagerViewModel Tags { get; }

    public GraphViewModel Graph { get; }

    public ImportLinkViewModel ImportLink { get; }

    public NewWorkspaceViewModel NewWorkspace { get; }

    public event Action? ImportLinkRequested;
    public event Action? NewWorkspaceRequested;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsEditorVisible { get; set; }

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    [ObservableProperty]
    public partial string NoteTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NoteContent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NoteTagsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PreviewMarkdown { get; set; } = string.Empty;

    public ObservableCollection<TagItem> AvailableTags { get; } = new();

    public async Task InitializeAsync()
    {
        await LoadAllAsync();
    }

    [RelayCommand]
    private void OpenSearch()
    {
        Search.Open();
    }

    [RelayCommand]
    private void CloseSearch()
    {
        Search.Close();
    }

    [RelayCommand]
    private async Task OpenTagManagerAsync()
    {
        await Tags.LoadAsync();
        TagManagerRequested?.Invoke();
    }

    public event Action? TagManagerRequested;

    [RelayCommand]
    private async Task OpenGraphAsync()
    {
        await Graph.LoadAsync();
        GraphRequested?.Invoke();
    }

    public event Action? GraphRequested;

    [RelayCommand]
    private void OpenImportLink()
    {
        if (_currentWorkspaceId is null)
        {
            StatusMessage = "Select a workspace first.";
            return;
        }

        ImportLinkRequested?.Invoke();
    }

    private async void OnGraphNodeSelected(Guid noteId)
    {
        await LoadNoteIntoEditorAsync(noteId);
    }

    private async void OnSearchItemSelected(NoteSearchItem item)
    {
        Search.Close();
        await LoadNoteIntoEditorAsync(item.Id);
    }

    private async Task LoadAllAsync()
    {
        IsBusy = true;
        StatusMessage = "Loading knowledge base...";
        try
        {
            await Tree.LoadAsync();
            await LoadTagsAsync();
            StatusMessage = "Ready";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadTagsAsync()
    {
        var tags = await _api.GetTagsAsync() ?? [];
        AvailableTags.Clear();
        foreach (var tag in tags)
        {
            AvailableTags.Add(new TagItem(tag.Id, tag.Name));
        }
    }

    private async void OnWorkspaceSelected(WorkspaceNode? workspace)
    {
        _currentWorkspaceId = workspace?.Id;
        _selectedWorkspace = workspace;
        IsEditorVisible = false;
    }

    private async void OnNoteSelected(NoteNode? node)
    {
        if (node is null)
        {
            IsEditorVisible = false;
            return;
        }

        IsBusy = true;
        try
        {
            await LoadNoteIntoEditorAsync(node.Id);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load note: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadNoteIntoEditorAsync(Guid noteId)
    {
        var note = await _api.GetNoteAsync(noteId);
        if (note is null)
        {
            StatusMessage = "Note no longer exists.";
            IsEditorVisible = false;
            return;
        }

        _editingNoteId = note.Id;
        _currentWorkspaceId = note.WorkspaceId;
        NoteTitle = note.Title;
        NoteContent = note.ContentMarkdown;
        NoteTagsText = string.Join(", ", note.Tags.Select(t => t.Name));
        PreviewMarkdown = note.ContentMarkdown;
        IsEditing = true;
        IsEditorVisible = true;
        StatusMessage = $"Opened note '{note.Title}'.";
    }

    [RelayCommand]
    private void TogglePreview()
    {
        PreviewMarkdown = NoteContent;
    }

    [RelayCommand]
    private async Task NewNoteAsync()
    {
        if (_currentWorkspaceId is null)
        {
            StatusMessage = "Select a workspace first.";
            return;
        }

        _editingNoteId = null;
        NoteTitle = string.Empty;
        NoteContent = string.Empty;
        NoteTagsText = string.Empty;
        PreviewMarkdown = string.Empty;
        IsEditing = false;
        IsEditorVisible = true;
        StatusMessage = "New note - fill the editor and click Save.";
    }

    [RelayCommand]
    private async Task SaveNoteAsync()
    {
        if (_currentWorkspaceId is null)
        {
            StatusMessage = "Select a workspace first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NoteTitle))
        {
            StatusMessage = "A title is required.";
            return;
        }

        var tags = TagsFromText(NoteTagsText);

        IsBusy = true;
        try
        {
            var tagsList = tags.ToList();

            if (_editingNoteId is Guid editingId)
            {
                await _api.UpdateNoteAsync(editingId, new UpdateNoteRequest(NoteTitle, NoteContent, tagsList));
                StatusMessage = "Note updated.";
            }
            else
            {
                var id = await _api.CreateNoteAsync(_currentWorkspaceId!.Value, new CreateNoteRequest(NoteTitle, NoteContent, tagsList));
                _editingNoteId = id;
                StatusMessage = "Note created.";
            }

            PreviewMarkdown = NoteContent;
            await Tree.LoadAsync();
            await LoadTagsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteNoteAsync()
    {
        if (_editingNoteId is not Guid id)
        {
            StatusMessage = "No note selected to delete.";
            return;
        }

        IsBusy = true;
        try
        {
            await _api.DeleteNoteAsync(id);
            _editingNoteId = null;
            IsEditorVisible = false;
            StatusMessage = "Note deleted.";
            await Tree.LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenNewWorkspace()
    {
        NewWorkspaceRequested?.Invoke();
    }

    [RelayCommand]
    private async Task DeleteWorkspaceAsync()
    {
        if (_selectedWorkspace is not { } workspace)
        {
            StatusMessage = "Select a workspace to delete.";
            return;
        }

        IsBusy = true;
        try
        {
            await _api.DeleteWorkspaceAsync(workspace.Id);
            IsEditorVisible = false;
            StatusMessage = $"Workspace '{workspace.Name}' deleted.";
            await Tree.LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAllAsync();

    private static IEnumerable<string> TagsFromText(string text) =>
        text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);
}

public sealed record TagItem(Guid Id, string Name);