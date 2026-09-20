using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KnowledgeBase.Desktop.Models;
using KnowledgeBase.Desktop.Services;

namespace KnowledgeBase.Desktop.ViewModels;

public sealed partial class TagManagerViewModel : ViewModelBase
{
    private readonly IKnowledgeBaseApiClient _api;
    private string _editingId = string.Empty;

    public TagManagerViewModel(IKnowledgeBaseApiClient api)
    {
        _api = api;
    }

    public ObservableCollection<TagRow> Tags { get; } = new();

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewTagName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewTagColor { get; set; } = "#FFB020";

    [ObservableProperty]
    public partial string EditColor { get; set; } = "#FFB020";

    public IBrush NewTagColorBrush => new SolidColorBrush(ColorParsing.TryParse(NewTagColor, 255, 208, 120));

    public IBrush EditColorBrush => new SolidColorBrush(ColorParsing.TryParse(EditColor, 160, 160, 160));

    partial void OnNewTagColorChanged(string value) => OnPropertyChanged(nameof(NewTagColorBrush));

    partial void OnEditColorChanged(string value) => OnPropertyChanged(nameof(EditColorBrush));

    [ObservableProperty]
    public partial TagRow? SelectedTag { get; set; }

    [ObservableProperty]
    public partial string EditName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MergeTargetName { get; set; } = string.Empty;

    public async Task LoadAsync()
    {
        var tags = await _api.GetTagsAsync() ?? [];
        Tags.Clear();
        foreach (var tag in tags)
        {
            Tags.Add(new TagRow(tag.Id, tag.Name, tag.Color, tag.NoteCount));
        }
    }

    partial void OnSelectedTagChanged(TagRow? value)
    {
        if (value is null)
        {
            return;
        }

        _editingId = value.Id.ToString();
        EditName = value.Name;
        EditColor = value.Color;
    }

    [RelayCommand]
    private async Task AddTagAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTagName))
        {
            StatusMessage = "Enter a tag name.";
            return;
        }

        try
        {
            await _api.CreateTagAsync(NewTagName.Trim(), NewTagColor);
            NewTagName = string.Empty;
            StatusMessage = "Tag created.";
            await LoadAsync();
            ApplyMergeOptions();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Create failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task UpdateTagAsync()
    {
        if (SelectedTag is null || _editingId.Length == 0)
        {
            StatusMessage = "Select a tag to edit.";
            return;
        }

        try
        {
            await _api.UpdateTagAsync(SelectedTag.Id, EditName.Trim(), EditColor);
            StatusMessage = "Tag updated.";
            await LoadAsync();
            ApplyMergeOptions();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Update failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task MergeTagAsync()
    {
        if (SelectedTag is null || string.IsNullOrWhiteSpace(MergeTargetName))
        {
            StatusMessage = "Select a tag and a merge target.";
            return;
        }

        var target = Tags.FirstOrDefault(t => t.Name == MergeTargetName.Trim());
        if (target is null)
        {
            StatusMessage = $"Tag '{MergeTargetName}' not found.";
            return;
        }

        if (target.Id == SelectedTag.Id)
        {
            StatusMessage = "Can't merge a tag into itself.";
            return;
        }

        try
        {
            await _api.MergeTagAsync(SelectedTag.Id, target.Id);
            await LoadAsync();
            ApplyMergeOptions();
            StatusMessage = $"Merged '{SelectedTag.Name}' into '{target.Name}'.";
            SelectedTag = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Merge failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteTagAsync()
    {
        if (SelectedTag is null)
        {
            StatusMessage = "Select a tag to delete.";
            return;
        }

        try
        {
            await _api.DeleteTagAsync(SelectedTag.Id);
            await LoadAsync();
            ApplyMergeOptions();
            StatusMessage = $"Deleted '{SelectedTag.Name}'.";
            SelectedTag = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
        }
    }

    private void ApplyMergeOptions()
    {
        // refresh merge target default input placeholder if it referenced a deleted tag
        if (MergeTargetName.Length > 0 && !Tags.Any(t => t.Name == MergeTargetName))
        {
            MergeTargetName = string.Empty;
        }
    }
}

public sealed record TagRow(Guid Id, string Name, string Color, int NoteCount)
{
    public string DisplayName => $"{Name} ({NoteCount})";
    public IBrush ColorBrush => new SolidColorBrush(ColorParsing.TryParse(Color, 160, 160, 160));
}