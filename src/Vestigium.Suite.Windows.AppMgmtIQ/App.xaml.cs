using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Vestigium.Controls.DependencyInjection;
using System.ComponentModel;
using Vestigium.Controls.Shell;
using Vestigium.Controls.StatusBar;
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
        MainWindow = CreateMainWindow(out var inventory, out var features, out var bloatware, out var setup, out var startup, out var chrome, out var dashboard);
        MainWindow.Show();

        startup.Attach(inventory);
        await Task.WhenAll(inventory.LoadAsync(), features.LoadAsync(), bloatware.LoadAsync(), setup.LoadAsync(), startup.LoadAsync());
        startup.Link(inventory.AllPackages);
        chrome.Status.Message = inventory.Status;
        dashboard.Bind(inventory, features, bloatware, setup);
        WireStatus(chrome, inventory, features, bloatware, setup, startup);

        base.OnStartup(e);
    }

    private static void WireStatus(
        VestigiumDefaultWindowViewModel chrome,
        MainViewModel inventory,
        FeaturesViewModel features,
        BloatwareViewModel bloatware,
        ActiveSetupViewModel setup,
        StartupViewModel startup)
    {
        chrome.Status.Engine.Columns.Add(new StatusBarColumn
        {
            Key = "summary",
            Kind = StatusBarColumnKind.Text,
            Slot = StatusBarSlot.Center
        });

        void Update()
        {
            var title = Shell?.SelectedItem?.Header;
            var text = title switch
            {
                "AppMgmtIQ" => inventory.Status,
                "Windows Features" => features.Status,
                "Bloatware" => bloatware.Status,
                "Active Setup" => setup.Status,
                "Startup" => startup.Status,
                _ => ""
            };
            chrome.Status.Engine.PostImmediate("summary", new StatusBarUpdate { Text = text });
        }

        inventory.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(MainViewModel.Status)) Update(); };
        features.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(FeaturesViewModel.Status)) Update(); };
        bloatware.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(BloatwareViewModel.Status)) Update(); };
        setup.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ActiveSetupViewModel.Status)) Update(); };
        startup.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(StartupViewModel.Status)) Update(); };
        if (Shell is not null)
            DependencyPropertyDescriptor.FromProperty(VestigiumShell.SelectedItemProperty, typeof(VestigiumShell))
                .AddValueChanged(Shell, (_, _) => Update());
        Update();
    }

    private static Window CreateMainWindow(
        out MainViewModel inventory,
        out FeaturesViewModel featuresVm,
        out BloatwareViewModel bloatware,
        out ActiveSetupViewModel setup,
        out StartupViewModel startup,
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
                new VestigiumNavItemSpec("Bloatware")
                {
                    Title = "Bloatware",
                    Subject = "AppX catalog",
                    Description = "Known consumer packages matched against what is installed."
                },
                new VestigiumNavItemSpec("Active Setup")
                {
                    Title = "Active Setup",
                    Subject = "Logon components",
                    Description = "Installed Components from HKLM and HKCU, both views."
                },
                new VestigiumNavItemSpec("Startup")
                {
                    Title = "Startup",
                    Subject = "Persistence",
                    Description = "Run, RunOnce, RunOnceEx, and Startup folder entries."
                },
                new VestigiumNavItemSpec("Dashboard")
                {
                    Title = "Dashboard",
                    Subject = "Inventory",
                    Description = "Counts and charts for the inventory."
                },
                new VestigiumNavItemSpec("Export")
                {
                    Title = "Export",
                    Subject = "Workbook",
                    Description = "Write packages and features to Excel."
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
        bloatware = new BloatwareViewModel { IsLoading = true };
        setup = new ActiveSetupViewModel { IsLoading = true };
        startup = new StartupViewModel { IsLoading = true };
        var store = new AppMgmtSettingsStore(AppMgmtSettingsStore.DefaultRoot);
        var settings = new SettingsViewModel(Themes, inventory, store);
        settings.BindMrus(featuresVm, bloatware, setup, startup);
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
        {
            var featuresView = new FeaturesView { DataContext = featuresVm };
            featuresView.Attach(settings);
            features.Content = featuresView;
        }

        var bloat = window.HostShell["Bloatware"];
        if (bloat is not null)
            bloat.Content = new BloatwareView { DataContext = bloatware };

        var active = window.HostShell["Active Setup"];
        if (active is not null)
            active.Content = new ActiveSetupView { DataContext = setup };

        var start = window.HostShell["Startup"];
        if (start is not null)
            start.Content = new StartupView { DataContext = startup };

        var export = window.HostShell["Export"];
        if (export is not null)
            export.Content = new ExportView { DataContext = new ExportViewModel(inventory, featuresVm, bloatware, setup, store) };

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
