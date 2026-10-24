using System.Windows;
using Vestigium.Controls.Shell;
using Vestigium.Controls.UnderConstruction;
using Vestigium.Themes;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public partial class App : Application
{
    public static ThemeManager Themes { get; } = new();

    public static FXplorerSettingsStore Settings { get; } = new(FXplorerSettingsStore.DefaultRoot);

    public static VestigiumShell? Shell { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        HostLog.Initialize(HostIds.FXplorerIQ);
        HostLog.Information(FXplorerCatalog.AppStarted, "Lifecycle", "FXplorerIQ opened.");
        Themes.RegisterSuiteV1();
        Themes.Initialize(this, SavedTheme());

        var jobPath = JobPath(e.Args);
        if (jobPath is not null)
        {
            RunJob(jobPath);
            base.OnStartup(e);
            return;
        }

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        MainWindow = CreateMainWindow();
        MainWindow.Show();

        base.OnStartup(e);
    }

    private static string? JobPath(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], "--job", StringComparison.OrdinalIgnoreCase))
                continue;
            return i + 1 < args.Length ? args[i + 1] : null;
        }
        return null;
    }

    private void RunJob(string path)
    {
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        var job = ElevatedJob.Read(path);
        if (job is null)
        {
            Shutdown(2);
            return;
        }

        var work = job.Build();
        var window = new JobWindow(job.Verb, work, job.Source, job.Destination)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        MainWindow = window;
        window.ShowDialog();
        ElevatedJob.WriteResult(path, window.Result);
        Shutdown(window.Result?.Status == "Success" ? 0 : 1);
    }

    private static string SavedTheme()
    {
        var id = Settings.Load().ThemeId;
        if (string.IsNullOrWhiteSpace(id))
            return "LightBlue";
        return Themes.AvailableThemes.Any(theme => string.Equals(theme.Id, id, StringComparison.OrdinalIgnoreCase))
            ? id
            : "LightBlue";
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

        Set(window.HostShell, "Dashboard", new VestigiumUnderConstruction
        {
            Title = "Dashboard",
            Subject = "Later",
            Description = "Counts of ran, failed, and bytes moved."
        });
        Set(window.HostShell, "Settings", new SettingsView());
        Set(window.HostShell, "Help", new VestigiumUnderConstruction
        {
            Title = "Help",
            Subject = "About",
            Description = "Explorers, verbs, queue, and cancel."
        });

        window.Start(text => chrome.Status.Message = text);
        return window;
    }

    private static void Set(VestigiumShell shell, string header, object content)
    {
        var item = shell[header];
        if (item is not null)
            item.Content = content;
    }
}
