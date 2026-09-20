using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using KnowledgeBase.Desktop.Configuration;
using KnowledgeBase.Desktop.Services;
using KnowledgeBase.Desktop.ViewModels;
using KnowledgeBase.Desktop.Views;

namespace KnowledgeBase.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var apiOptions = DesktopConfiguration.LoadApiOptions();
            var api = new KnowledgeBaseApiClient(apiOptions.BaseUrl);
            var viewModel = new MainViewModel(api);
            viewModel.InitializeAsync().GetAwaiter().GetResult();

            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}