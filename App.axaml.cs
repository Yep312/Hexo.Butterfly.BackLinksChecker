using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Hexo.Butterfly.BackLinksChecker
{
    public partial class App : Application
    {
        public static MainWindow MainWindow { get; set; } = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var w =  new MainWindow();
                desktop.MainWindow = w;
                MainWindow = w;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}