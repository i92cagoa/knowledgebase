using Avalonia.Controls;
using KnowledgeBase.Desktop.ViewModels;

namespace KnowledgeBase.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.TagManagerRequested += () =>
                {
                    var window = new TagsWindow
                    {
                        DataContext = vm.Tags
                    };
                    window.ShowDialog(this);
                };

                vm.GraphRequested += () =>
                {
                    var window = new GraphWindow
                    {
                        DataContext = vm.Graph
                    };
                    window.ShowDialog(this);
                };

                vm.ImportLinkRequested += () =>
                {
                    var window = new ImportLinkWindow
                    {
                        DataContext = vm.ImportLink
                    };
                    vm.ImportLink.CloseRequested += window.Close;
                    window.ShowDialog(this);
                };

                vm.NewWorkspaceRequested += () =>
                {
                    var window = new NewWorkspaceWindow
                    {
                        DataContext = vm.NewWorkspace
                    };
                    vm.NewWorkspace.CloseRequested += window.Close;
                    window.ShowDialog(this);
                };
            }
        };
    }
}