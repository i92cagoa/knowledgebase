using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KnowledgeBase.Desktop.Models;
using KnowledgeBase.Desktop.Services;

namespace KnowledgeBase.Desktop.ViewModels;

public sealed partial class SearchViewModel : ViewModelBase
{
    private readonly IKnowledgeBaseApiClient _api;
    private readonly Action<NoteSearchItem> _openNote;
    private CancellationTokenSource? _debounceCts;

    public SearchViewModel(IKnowledgeBaseApiClient api, Action<NoteSearchItem> openNote)
    {
        _api = api;
        _openNote = openNote;
    }

    public ObservableCollection<NoteSearchItem> Results { get; } = new();

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    [ObservableProperty]
    public partial NoteSearchItem? SelectedItem { get; set; }

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    public partial bool HasResults { get; set; }

    [ObservableProperty]
    public partial bool NoResults { get; set; }

    partial void OnSearchTextChanged(string value)
    {
        _debounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _debounceCts = cts;

        _ = SearchWithDebounceAsync(cts.Token);
    }

    private async Task SearchWithDebounceAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(250, token);

            await SearchAsync();
        }
        catch (OperationCanceledException)
        {
            // superseded by a newer keystroke
        }
    }

    public async Task SearchAsync()
    {
        try
        {
            var query = (SearchText ?? "").Trim();
            var page = await _api.SearchNotesAsync(query, null, 1, 20);

            if (page is null)
            {
                ResetResults();
                return;
            }

            Results.Clear();
            foreach (var item in page.Items)
            {
                Results.Add(item);
            }

            TotalCount = page.TotalCount;
            HasResults = Results.Count > 0;
            NoResults = !HasResults;

            SelectedItem = Results.FirstOrDefault();
        }
        catch (Exception)
        {
            ResetResults();
            NoResults = true;
            HasResults = false;
        }
    }

    private void ResetResults()
    {
        Results.Clear();
        TotalCount = 0;
        HasResults = false;
        NoResults = false;
        SelectedItem = null;
    }

    [RelayCommand]
    private void OpenSelected()
    {
        if (SelectedItem is not null)
        {
            _openNote(SelectedItem);
            Close();
        }
    }

    public void Open()
    {
        IsOpen = true;
        SearchText = string.Empty;
        ResetResults();
    }

    public void Close()
    {
        IsOpen = false;
        _debounceCts?.Cancel();
        ResetResults();
    }
}