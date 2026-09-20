using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Media;
using Avalonia.VisualTree;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Input;
using KnowledgeBase.Desktop;
using KnowledgeBase.Desktop.Models;
using KnowledgeBase.Desktop.ViewModels;
using KnowledgeBase.Desktop.Views;

namespace KnowledgeBase.DesktopTests;

/// <summary>
/// Regression tests for previously fixed bugs, to prevent them being reintroduced.
/// </summary>
public sealed class RegressionTests : IAsyncLifetime, IDisposable
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

    // ---- bug: clicking "New Workspace" did nothing (parameterized command, no UI) ----

    [Fact]
    public async Task NewWorkspace_Button_Raises_Event_And_Can_Create()
    {
        var api = new FakeKnowledgeBaseApiClient();
        var vm = new MainViewModel(api);
        var raised = false;
        vm.NewWorkspaceRequested += () => raised = true;

        vm.OpenNewWorkspaceCommand.Execute(null);
        raised.Should().BeTrue("clicking New Workspace must open a dialog");

        var dialog = new NewWorkspaceViewModel(api, () => { });
        var closed = false;
        dialog.CloseRequested += () => closed = true;
        dialog.Name = "Inbox";
        await ((IAsyncRelayCommand)dialog.CreateCommand).ExecuteAsync(null);
        api.CreateWorkspaceCalls.Should().Be(1);
        closed.Should().BeTrue();
    }

    // ---- bug: "Not found" shown instead of note titles in the tree ----

    [Fact]
    public async Task Tree_Does_Not_Fall_Back_To_ViewLocator_For_Notes()
    {
        var api = new FakeKnowledgeBaseApiClient();
        api.Tree.Clear();
        api.Tree.Add(new WorkspaceTree(
            Guid.NewGuid(),
            "Architecture",
            [new NoteSummary(Guid.NewGuid(), "EF Core caching", DateTime.UtcNow, ["dotnet"])]));

        var texts = await RenderAsync(api, editorVisible: false);

        var hasNotFound = texts.Any(t => t is not null && t.Contains("Not Found"));
        texts.Should().Contain("Architecture");
        hasNotFound.Should().BeFalse("tree must not fall back to the ViewLocator 'Not Found' text");
    }

    // ---- bug: application froze / was not responding at startup ----
    // The window must be shown before the background load; init must not block.

    [Fact]
    public async Task InitializeAsync_Does_Not_Throw_And_Loads()
    {
        var api = new FakeKnowledgeBaseApiClient();
        var vm = new MainViewModel(api);

        await vm.InitializeAsync(); // must complete without blocking/exception

        vm.Tree.Workspaces.Should().NotBeEmpty();
        vm.StatusMessage.Should().NotBeEmpty();
    }

    // ---- bug: Save button was in the top toolbar and invisible; now it belongs to the editor ----

    [Fact]
    public async Task Save_Note_Not_In_Top_Toolbar_When_Editor_Hidden()
    {
        var api = new FakeKnowledgeBaseApiClient();
        var texts = await RenderAsync(api, editorVisible: false);

        // Top toolbar no longer holds a Save button.
        texts.Should().NotContain("Save Note");
        texts.Should().NotContain("Save Note.");
    }

    [Fact]
    public async Task Cancel_Discards_Editor_State()
    {
        var api = new FakeKnowledgeBaseApiClient();
        var vm = new MainViewModel(api);
        await vm.InitializeAsync();

        vm.Tree.SelectedItem = vm.Tree.Workspaces.First();
        await ((IAsyncRelayCommand)vm.NewNoteCommand).ExecuteAsync(null);
        vm.NoteTitle = "draft title";
        vm.NoteContent = "# draft";
        vm.IsEditorVisible.Should().BeTrue();

        vm.CancelNoteCommand.Execute(null);

        vm.IsEditorVisible.Should().BeFalse("Cancel must hide the editor");
        vm.NoteTitle.Should().BeEmpty();
        vm.NoteContent.Should().BeEmpty();
        vm.IsEditing.Should().BeFalse();
    }

    // ---- bug: tag color was shown only as a hex value; now the chip shows the actual color ----

    [Fact]
    public void TagItem_ColorBrush_Parses_Hex()
    {
        var tag = new TagItem(Guid.NewGuid(), "dotnet", "#512BD4");

        var brush = tag.ColorBrush as ISolidColorBrush;
        brush.Should().NotBeNull();
        brush!.Color.Should().Be(Color.Parse("#512BD4"));
    }

    [Fact]
    public void TagItem_ColorBrush_Falls_Back_On_Invalid_Hex()
    {
        var tag = new TagItem(Guid.NewGuid(), "x", "");

        var brush = tag.ColorBrush as ISolidColorBrush;
        brush.Should().NotBeNull();
    }

    // ---- bug: app width was too narrow for all buttons; must be wide + a min width ----

    [Fact]
    public void Window_Is_Wide_Enough_For_Toolbar()
    {
        var xaml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Views", "MainWindow.axaml"));

        xaml.Should().Contain("Width=\"1440\"");
        xaml.Should().Contain("MinWidth=\"1000\"");
    }

    // --------------------------------------------------------------- helpers

    private async Task<string?[]> RenderAsync(FakeKnowledgeBaseApiClient api, bool editorVisible)
    {
        string?[] captured = [];
        await _session!.Dispatch(
            () =>
            {
                var vm = new MainViewModel(api);
                vm.InitializeAsync().GetAwaiter().GetResult();

                if (editorVisible)
                {
                    vm.Tree.SelectedItem = vm.Tree.Workspaces.First();
                    vm.NewNoteCommand.Execute(null);
                }

                var window = new MainWindow { DataContext = vm };
                window.Show();
                window.Measure(Size.Infinity);
                window.Arrange(new Rect(window.DesiredSize));
                Thread.Sleep(80);

                var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToArray();
                window.Close();
                captured = texts;
            },
            CancellationToken.None);

        return captured;
    }
}
/// <summary>Retro button styling must be applied application-wide.</summary>
public sealed class RetroStyleTests : IAsyncLifetime, IDisposable
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

    [Fact]
    public async Task App_Styles_Contain_Retro_Button_Theme()
    {
        var captured = false;
        await _session!.Dispatch(
            () =>
            {
                var app = Application.Current;
                captured = app?.Styles.OfType<Avalonia.Styling.Style>()
                    .Any(st => st.Selector is not null && st.Selector.ToString()!.StartsWith("Button")) == true;
            },
            CancellationToken.None);

        captured.Should().BeTrue("a retro Button style must be registered in Application.Styles");
    }
}
