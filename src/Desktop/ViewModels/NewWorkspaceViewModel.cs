using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KnowledgeBase.Desktop.Services;

namespace KnowledgeBase.Desktop.ViewModels;

public sealed partial class NewWorkspaceViewModel : ViewModelBase
{
    private readonly IKnowledgeBaseApiClient _api;
    private readonly Action _onCreated;

    public NewWorkspaceViewModel(IKnowledgeBaseApiClient api, Action onCreated)
    {
        _api = api;
        _onCreated = onCreated;
    }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public event Action? CloseRequested;

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "Enter a workspace name.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Creating workspace...";
        try
        {
            await _api.CreateWorkspaceAsync(Name.Trim(), string.IsNullOrWhiteSpace(Description) ? null : Description.Trim());
            StatusMessage = "Workspace created.";
            _onCreated();
            CloseRequested?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Create failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();
}