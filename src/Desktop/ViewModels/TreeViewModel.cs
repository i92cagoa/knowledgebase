using System.Collections.ObjectModel;
using System.Threading.Tasks;
using KnowledgeBase.Desktop.Services;

namespace KnowledgeBase.Desktop.ViewModels;

public sealed class WorkspaceNode : ViewModelBase
{
    public Guid Id { get; }
    public string Name { get; }

    public IReadOnlyList<NoteNode> Notes { get; }

    public WorkspaceNode(Guid id, string name, IReadOnlyList<NoteNode> notes)
    {
        Id = id;
        Name = name;
        Notes = notes;
    }
}

public sealed class NoteNode : ViewModelBase
{
    public Guid Id { get; }
    public string Title { get; }
    public string TagsSummary { get; }
    public bool HasTags => TagsSummary.Length > 0;

    public NoteNode(Guid id, string title, IReadOnlyList<string> tags)
    {
        Id = id;
        Title = title;
        TagsSummary = string.Join(", ", tags);
    }
}

public sealed partial class TreeViewModel : ViewModelBase
{
    private readonly IKnowledgeBaseApiClient _api;
    private object? _selectedItem;

    public TreeViewModel(IKnowledgeBaseApiClient api)
    {
        _api = api;
    }

    public ObservableCollection<WorkspaceNode> Workspaces { get; } = new();

    public object? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                PublishSelection();
            }
        }
    }

    public event Action<WorkspaceNode?>? WorkspaceSelectionChanged;
    public event Action<NoteNode?>? NoteSelectionChanged;

    private void PublishSelection()
    {
        switch (_selectedItem)
        {
            case NoteNode note:
                WorkspaceSelectionChanged?.Invoke(null);
                NoteSelectionChanged?.Invoke(note);
                break;
            case WorkspaceNode workspace:
                NoteSelectionChanged?.Invoke(null);
                WorkspaceSelectionChanged?.Invoke(workspace);
                break;
            default:
                WorkspaceSelectionChanged?.Invoke(null);
                NoteSelectionChanged?.Invoke(null);
                break;
        }
    }

    public async Task LoadAsync()
    {
        var tree = await _api.GetTreeAsync() ?? [];

        Workspaces.Clear();
        foreach (var workspace in tree)
        {
            var notes = workspace.Notes
                .Select(n => new NoteNode(n.Id, n.Title, n.Tags))
                .ToList();
            Workspaces.Add(new WorkspaceNode(workspace.Id, workspace.Name, notes));
        }
    }
}