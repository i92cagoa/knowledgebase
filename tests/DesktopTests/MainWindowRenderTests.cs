using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.VisualTree;
using AwesomeAssertions;
using KnowledgeBase.Desktop;
using KnowledgeBase.Desktop.Models;
using KnowledgeBase.Desktop.ViewModels;
using KnowledgeBase.Desktop.Views;

namespace KnowledgeBase.DesktopTests;

/// <summary>
/// Renders the main window in an Avalonia headless session and verifies:
///  - the tree uses real data templates for workspace + note nodes (so notes never fall back
///    to the ViewLocator "Not Found..." text)
///  - the Save/Cancel buttons appear in the editor
/// </summary>
public sealed class MainWindowRenderTests : IAsyncLifetime, IDisposable
{
    private HeadlessUnitTestSession? _session;

    public async Task InitializeAsync()
    {
        _session = HeadlessUnitTestSession.StartNew(typeof(App));
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _session?.Dispose();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// The regression guard for the "Not Found" bug: both node types must be covered by a
    /// data template on the TreeView so the app-level ViewLocator fallback is never reached.
    /// </summary>
    [Fact]
    public async Task Tree_DataTemplates_Cover_Workspace_And_Note_Nodes()
    {
        var templates = await TemplateInfoAsync();

        templates.Should().ContainKey(typeof(WorkspaceNode), "workspace nodes need a TreeDataTemplate");
        templates.Should().ContainKey(typeof(NoteNode), "note nodes need a DataTemplate (was falling back to ViewLocator 'Not Found...')");
    }

    /// <summary>
    /// The workspace node itself renders (top-level tree row) without raising the
    /// "Not Found" fallback text anywhere in the window.
    /// </summary>
    [Fact]
    public async Task Rendered_Window_Shows_Tree_Without_NotFound_Fallback()
    {
        var api = new FakeKnowledgeBaseApiClient();
        api.Tree.Clear();
        api.Tree.Add(new WorkspaceTree(Guid.NewGuid(), "Architecture", []));

        var texts = await RenderTextsAsync(api);

        texts.Should().Contain("Architecture");
        texts.Should().NotContain("Not Found");
    }

    [Fact]
    public async Task Save_And_Cancel_Buttons_Appear_In_Editor()
    {
        var api = new FakeKnowledgeBaseApiClient();
        var texts = await RenderTextsAsync(api, seedEditor: true);

        texts.Should().Contain("Save Note");
        texts.Should().Contain("Cancel");
    }

    private async Task<IReadOnlyDictionary<Type, bool>> TemplateInfoAsync()
    {
        Dictionary<Type, bool> captured = [];
        await _session!.Dispatch(
            () =>
            {
                var window = new MainWindow();
                window.Show();
                window.Measure(Size.Infinity);
                window.Arrange(new Rect(window.DesiredSize));

                var tree = window.GetVisualDescendants().OfType<TreeView>().FirstOrDefault();
                var templates = tree?.DataTemplates ?? [];

                captured[typeof(WorkspaceNode)] = templates.Any(t => t.Match(new WorkspaceNode(Guid.NewGuid(), "x", [])));
                captured[typeof(NoteNode)] = templates.Any(t => t.Match(new NoteNode(Guid.NewGuid(), "y", [])));

                window.Close();
            },
            CancellationToken.None);

        return captured;
    }

    private async Task<string?[]> RenderTextsAsync(FakeKnowledgeBaseApiClient api, bool seedEditor = false)
    {
        string?[] captured = [];
        await _session!.Dispatch(
            () =>
            {
                var vm = new MainViewModel(api);
                vm.InitializeAsync().GetAwaiter().GetResult();

                if (seedEditor)
                {
                    vm.Tree.SelectedItem = vm.Tree.Workspaces.First();
                    vm.NewNoteCommand.Execute(null);
                }

                var window = new MainWindow { DataContext = vm };
                window.Show();
                window.Measure(Size.Infinity);
                window.Arrange(new Rect(window.DesiredSize));
                Thread.Sleep(100);

                captured = window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToArray();
                window.Close();
            },
            CancellationToken.None);

        return captured;
    }
}