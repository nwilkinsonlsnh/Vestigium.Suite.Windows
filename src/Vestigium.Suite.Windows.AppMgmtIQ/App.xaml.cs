using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Vestigium.Controls.DependencyInjection;
using Vestigium.Controls.Shell;
using Vestigium.Converters.DependencyInjection;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;
using Vestigium.Suite.Windows.AppMgmtIQ.Views;
using Vestigium.Themes;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public partial class App : Application
{
    public static ThemeManager Themes { get; } = new();

    public static IServiceProvider Services { get; private set; } = null!;

    public static VestigiumShell? Shell { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        HostLog.Initialize(HostIds.AppMgmtIQ);
        HostLog.Information(AppMgmtCatalog.AppStarted, "Lifecycle", "AppMgmtIQ started.");

        Themes.RegisterSuiteV1();
        Themes.Initialize(this, "LightBlue");

        var services = new ServiceCollection();
        services.AddVestigiumConverters();
        services.AddVestigiumControls();
        Services = services.BuildServiceProvider();

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        MainWindow = CreateMainWindow();
        MainWindow.Show();

        base.OnStartup(e);
    }

    private static Window CreateMainWindow()
    {
        var chrome = Services.GetRequiredService<VestigiumDefaultWindowViewModel>();
        var window = new AppMgmtIqWindow(chrome);
        Shell = window.HostShell;

        window.HostShell.ApplySpec(new VestigiumShellSpec
        {
            Items =
            {
                new VestigiumNavItemSpec("AppMgmtIQ")
                {
                    Title = "AppMgmtIQ",
                    Subject = "Installed packages",
                    Description = "Grid of every package from the four ARP roots."
                },
                new VestigiumNavItemSpec("Settings")
                {
                    Title = "Settings",
                    Subject = "Display",
                    Description = "Theme and display options."
                },
                new VestigiumNavItemSpec("Help")
                {
                    Title = "Help",
                    Subject = "About",
                    Description = "How this window works."
                }
            }
        });

        var vm = new MainViewModel();
        vm.Load();
        var store = new AppMgmtSettingsStore(AppMgmtSettingsStore.DefaultRoot);
        var settings = new SettingsViewModel(Themes, vm, store);
        settings.Load();

        var inventory = window.HostShell["AppMgmtIQ"];
        if (inventory is not null)
        {
            inventory.Content = new AppMgmtIqView { DataContext = vm };
            window.HostShell.SelectedItem = inventory;
        }

        var settingsItem = window.HostShell["Settings"];
        if (settingsItem is not null)
            settingsItem.Content = new SettingsView { DataContext = settings };

        var help = window.HostShell["Help"];
        if (help is not null)
            help.Content = new HelpView();

        chrome.Status.Message = vm.Status;
        return window;
    }
}
