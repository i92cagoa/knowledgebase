using Avalonia.Controls;

namespace KnowledgeBase.Desktop.Views;

public partial class TagsWindow : Window
{
    public TagsWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}