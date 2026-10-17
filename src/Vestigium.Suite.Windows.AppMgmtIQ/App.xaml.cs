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

    protected override async void OnStartup(StartupEventArgs e)
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
        MainWindow = CreateMainWindow(out var inventory, out var features, out var chrome, out var dashboard);
        MainWindow.Show();

        await Task.WhenAll(inventory.LoadAsync(), features.LoadAsync());
        chrome.Status.Message = inventory.Status;
        dashboard.Bind(inventory, features);

        base.OnStartup(e);
    }

    private static Window CreateMainWindow(
        out MainViewModel inventory,
        out FeaturesViewModel featuresVm,
        out VestigiumDefaultWindowViewModel chrome,
        out DashboardView dashboard)
    {
        chrome = Services.GetRequiredService<VestigiumDefaultWindowViewModel>();
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
                new VestigiumNavItemSpec("Windows Features")
                {
                    Title = "Windows Features",
                    Subject = "Optional features",
                    Description = "Tree of Win32_OptionalFeature."
                },
                new VestigiumNavItemSpec("Export")
                {
                    Title = "Export",
                    Subject = "Workbook",
                    Description = "Write packages and features to Excel."
                },
                new VestigiumNavItemSpec("Dashboard")
                {
                    Title = "Dashboard",
                    Subject = "Inventory",
                    Description = "Counts and charts for the inventory."
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

        inventory = new MainViewModel { IsLoading = true };
        featuresVm = new FeaturesViewModel { IsLoading = true };
        var store = new AppMgmtSettingsStore(AppMgmtSettingsStore.DefaultRoot);
        var settings = new SettingsViewModel(Themes, inventory, store);
        settings.Load();

        var page = new AppMgmtIqView { DataContext = inventory };
        page.Attach(settings);

        var item = window.HostShell["AppMgmtIQ"];
        if (item is not null)
        {
            item.Content = page;
            window.HostShell.SelectedItem = item;
        }

        var features = window.HostShell["Windows Features"];
        if (features is not null)
            features.Content = new FeaturesView { DataContext = featuresVm };

        var export = window.HostShell["Export"];
        if (export is not null)
            export.Content = new ExportView { DataContext = new ExportViewModel(inventory, featuresVm) };

        dashboard = new DashboardView();
        var dashboardItem = window.HostShell["Dashboard"];
        if (dashboardItem is not null)
            dashboardItem.Content = dashboard;

        var settingsItem = window.HostShell["Settings"];
        if (settingsItem is not null)
            settingsItem.Content = new SettingsView { DataContext = settings };

        var help = window.HostShell["Help"];
        if (help is not null)
            help.Content = new HelpView();

        chrome.Status.Message = "Reading installed packages\u2026";
        return window;
    }
}
