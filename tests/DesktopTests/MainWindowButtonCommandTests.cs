using System.Reflection;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Input;
using KnowledgeBase.Desktop.ViewModels;

namespace KnowledgeBase.DesktopTests;

/// <summary>
/// Guards against the class of bug where a button binds to a missing/stale command so a
/// click silently does nothing: every Command binding on the main window must resolve to a
/// command on MainViewModel, and executing each command with the right prerequisites must
/// produce its expected effect.
/// </summary>
public sealed class MainWindowButtonCommandTests
{
    private static readonly (string Binding, string XamlBytePath)[] WindowFiles =
    [
        ("MainWindow", "Views/MainWindow.axaml"),
        ("TagsWindow", "Views/TagsWindow.axaml"),
        ("GraphWindow", "Views/GraphWindow.axaml"),
        ("ImportLinkWindow", "Views/ImportLinkWindow.axaml"),
        ("NewWorkspaceWindow", "Views/NewWorkspaceWindow.axaml")
    ];

    [Fact]
    public void Every_Button_Command_Binding_Resolves_To_A_Command()
    {
        foreach (var (windowName, xamlPath) in WindowFiles)
        {
            var rootType = ResolveRootView(RootViewModelName(windowName));
            rootType.Should().NotBeNull($"DataContext type for {windowName} must exist");

            var instance = CreateInstance(rootType!);
            instance.Should().NotBeNull($"an instance of {rootType!.Name} must be constructible for tests");

            var bindings = ExtractCommandBindings(ReadXaml(xamlPath));

            if (windowName == "MainWindow")
            {
                // The toolbar must have clickable buttons - this is the primary guard.
                bindings.Should().NotBeEmpty("MainWindow must have buttons");
            }

            foreach (var binding in bindings)
            {
                var command = ResolveCommand(instance!, binding);
                command.Should().NotBeNull($"{windowName} button '{binding}' must resolve");
                command.Should().BeAssignableTo<IRelayCommand>($"'{binding}' must be a relay command");
            }
        }
    }

    [Fact]
    public async Task Executing_Toolbar_Commands_Produces_Expected_Effects()
    {
        var api = new FakeKnowledgeBaseApiClient();
        var vm = new MainViewModel(api);
        var events = new EventProbe(vm);
        await vm.InitializeAsync();

        await RunAsync(vm.RefreshCommand);
        vm.Tree.Workspaces.Should().NotBeEmpty("refresh must load the tree");

        await RunAsync(vm.OpenSearchCommand);
        vm.Search.IsOpen.Should().BeTrue();

        await RunAsync(vm.CloseSearchCommand);
        vm.Search.IsOpen.Should().BeFalse();

        await RunAsync(vm.OpenNewWorkspaceCommand);
        events.NewWorkspaceOpened.Should().BeTrue();

        await RunAsync(vm.OpenTagManagerCommand);
        events.TagManagerOpened.Should().BeTrue();

        await RunAsync(vm.OpenGraphCommand);
        events.GraphOpened.Should().BeTrue();

        // ------- import link requires a workspace selected -------
        vm.Tree.SelectedItem = vm.Tree.Workspaces.FirstOrDefault();
        await RunAsync(vm.OpenImportLinkCommand);
        events.ImportLinkOpened.Should().BeTrue();

        // ------- new note shows a blank editor -------
        await RunAsync(vm.NewNoteCommand);
        vm.IsEditorVisible.Should().BeTrue();
        vm.NoteTitle.Should().BeEmpty();
        vm.IsEditing.Should().BeFalse();

        // ------- save creates a note -------
        vm.NoteTitle = "Draft title";
        await RunAsync(vm.SaveNoteCommand);
        api.CreateNoteCalls.Should().Be(1);

        // ------- toggle preview -------
        vm.NoteContent = "# hello";
        await RunAsync(vm.TogglePreviewCommand);
        vm.PreviewMarkdown.Should().Be("# hello");

        // ------- select a note then delete it -------
        vm.Tree.SelectedItem = vm.Tree.Workspaces.First().Notes.First();
        await Task.Delay(50); // OnNoteSelected is async-void
        await RunAsync(vm.DeleteNoteCommand);
        api.DeleteNoteCalls.Should().Be(1);

        // ------- delete workspace -------
        vm.Tree.SelectedItem = vm.Tree.Workspaces.FirstOrDefault();
        await RunAsync(vm.DeleteWorkspaceCommand);
        api.DeleteWorkspaceCalls.Should().Be(1);
    }

    private static Task RunAsync(IRelayCommand command)
    {
        if (command is IAsyncRelayCommand asyncCommand)
        {
            return asyncCommand.ExecuteAsync(null);
        }

        command.Execute(null);
        return Task.CompletedTask;
    }

    // ------------------------------------------------------------- helpers

    private static string ReadXaml(string path) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, path));

    private static string[] ExtractCommandBindings(string xaml) =>
        Regex.Matches(xaml, @"Command=""\{Binding\s+([A-Za-z0-9_]+)\s*\}")
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToArray();

    private static Type? ResolveRootView(string typeName) =>
        typeof(MainViewModel).Assembly.GetType($"KnowledgeBase.Desktop.ViewModels.{typeName}");

    private static string RootViewModelName(string windowName) => windowName switch
    {
        "MainWindow" => "MainViewModel",
        "TagsWindow" => "TagManagerViewModel",
        "GraphWindow" => "GraphViewModel",
        "ImportLinkWindow" => "ImportLinkViewModel",
        "NewWorkspaceWindow" => "NewWorkspaceViewModel",
        _ => throw new ArgumentOutOfRangeException(nameof(windowName))
    };

    private static object? ResolveCommand(object instance, string binding)
    {
        var prop = instance.GetType().GetProperty(binding, BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(instance);
    }

    private static object? CreateInstance(Type type)
    {
        // View models are constructed with an IKnowledgeBaseApiClient; use the fake.
        return type.Name switch
        {
            nameof(MainViewModel) => new MainViewModel(new FakeKnowledgeBaseApiClient()),
            nameof(TagManagerViewModel) => new TagManagerViewModel(new FakeKnowledgeBaseApiClient()),
            nameof(GraphViewModel) => new GraphViewModel(new FakeKnowledgeBaseApiClient()),
            nameof(ImportLinkViewModel) => new ImportLinkViewModel(
                new FakeKnowledgeBaseApiClient(),
                () => Guid.NewGuid(),
                () => { }),
            nameof(NewWorkspaceViewModel) => new NewWorkspaceViewModel(
                new FakeKnowledgeBaseApiClient(),
                () => { }),
            _ => null
        };
    }

private sealed class EventProbe
{
    public bool NewWorkspaceOpened { get; private set; }
    public bool TagManagerOpened { get; private set; }
    public bool GraphOpened { get; private set; }
    public bool ImportLinkOpened { get; private set; }
    public bool SearchOpened { get; private set; }

    public EventProbe(MainViewModel vm)
    {
        vm.TagManagerRequested += () => TagManagerOpened = true;
        vm.GraphRequested += () => GraphOpened = true;
        vm.ImportLinkRequested += () => ImportLinkOpened = true;
        vm.NewWorkspaceRequested += () => NewWorkspaceOpened = true;
        vm.Search.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SearchViewModel.IsOpen) && vm.Search.IsOpen)
            {
                SearchOpened = true;
            }
        };
    }
}
}