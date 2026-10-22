using System.Windows;
using Vestigium.Controls.Shell;
using Vestigium.Controls.UnderConstruction;
using Vestigium.Themes;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public partial class App : Application
{
    public static ThemeManager Themes { get; } = new();

    public static VestigiumShell? Shell { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        HostLog.Initialize(HostIds.FXplorerIQ);
        Themes.RegisterSuiteV1();
        Themes.Initialize(this, "LightBlue");

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        MainWindow = CreateMainWindow();
        MainWindow.Show();

        base.OnStartup(e);
    }

    private static Window CreateMainWindow()
    {
        var chrome = new VestigiumDefaultWindowViewModel();
        chrome.Status.Message = "Ready";
        var window = new FXplorerIqWindow(chrome);
        Shell = window.HostShell;

        window.HostShell.ApplySpec(new VestigiumShellSpec
        {
            Items =
            {
                new VestigiumNavItemSpec("Explorer")
                {
                    Title = "Explorer",
                    Subject = "Files",
                    Description = "One pane."
                },
                new VestigiumNavItemSpec("Dashboard")
                {
                    Title = "Dashboard",
                    Subject = "Counts",
                    Description = "Jobs this host has run."
                },
                new VestigiumNavItemSpec("Settings")
                {
                    Title = "Settings",
                    Subject = "Display",
                    Description = "Theme and defaults."
                },
                new VestigiumNavItemSpec("Help")
                {
                    Title = "Help",
                    Subject = "About",
                    Description = "How this window works."
                }
            }
        });

        Set(window.HostShell, "Explorer", new VestigiumUnderConstruction
        {
            Title = "Explorer",
            Subject = "One pane",
            Description = "The listing arrives in the next step."
        });
        Set(window.HostShell, "Dashboard", new VestigiumUnderConstruction
        {
            Title = "Dashboard",
            Subject = "Later",
            Description = "Counts of ran, failed, and bytes moved."
        });
        Set(window.HostShell, "Settings", new VestigiumUnderConstruction
        {
            Title = "Settings",
            Subject = "Later",
            Description = "Theme, default verb, collision, and retry."
        });
        Set(window.HostShell, "Help", new VestigiumUnderConstruction
        {
            Title = "Help",
            Subject = "Later",
            Description = "Explorers, verbs, queue, and cancel."
        });

        var explorer = window.HostShell["Explorer"];
        if (explorer is not null)
            window.HostShell.SelectedItem = explorer;

        return window;
    }

    private static void Set(VestigiumShell shell, string header, object content)
    {
        var item = shell[header];
        if (item is not null)
            item.Content = content;
    }
}
