using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using ETStock.ViewModels;
using ETStock.Views;
using System.Linq;

namespace ETStock
{
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
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                var selectorVm = new PeriodSelectorViewModel();
                var selectorWindow = new PeriodSelectorWindow { DataContext = selectorVm };

                desktop.MainWindow = selectorWindow;

                selectorWindow.Closed += (_, _) =>
                {
                    if (!selectorVm.IsConfirmed)
                    {
                        desktop.Shutdown();
                        return;
                    }

                    var mainWindow = new MainWindow
                    {
                        DataContext = new MainWindowViewModel(selectorVm.SelectedYear, selectorVm.SelectedMonth),
                    };
                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}