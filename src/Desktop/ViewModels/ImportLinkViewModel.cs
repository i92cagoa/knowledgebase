using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KnowledgeBase.Desktop.Services;

namespace KnowledgeBase.Desktop.ViewModels;

public sealed partial class ImportLinkViewModel : ViewModelBase
{
    private readonly KnowledgeBaseApiClient _api;
    private readonly Func<Guid?> _getWorkspaceId;
    private readonly Action _onImported;

    public ImportLinkViewModel(KnowledgeBaseApiClient api, Func<Guid?> getWorkspaceId, Action onImported)
    {
        _api = api;
        _getWorkspaceId = getWorkspaceId;
        _onImported = onImported;
    }

    [ObservableProperty]
    public partial string Url { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public event Action? CloseRequested;

    [RelayCommand]
    private async Task ImportAsync()
    {
        var workspaceId = _getWorkspaceId();
        if (workspaceId is null)
        {
            StatusMessage = "Select a workspace first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Url))
        {
            StatusMessage = "Enter a URL.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Fetching and analyzing the link...";
        try
        {
            await _api.ImportLinkAsync(workspaceId.Value, Url.Trim());
            StatusMessage = "Link imported as a note.";
            Url = string.Empty;
            _onImported();
            CloseRequested?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();
}