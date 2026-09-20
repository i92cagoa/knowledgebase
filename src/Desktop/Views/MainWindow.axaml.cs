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
            }
        };
    }
}