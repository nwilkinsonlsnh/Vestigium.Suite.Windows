using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Vestigium.Controls.DependencyInjection;
using Vestigium.Controls.Shell;
using Vestigium.Converters.DependencyInjection;
using Vestigium.Themes;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public partial class App : Application
{
    public static ThemeManager Themes { get; } = new();

    public static IServiceProvider Services { get; private set; } = null!;

    public static VestigiumShell? Shell { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        HostLog.Initialize("AppMgmtIQ");

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
                }
            }
        });

        var item = window.HostShell["AppMgmtIQ"];
        if (item is not null)
            window.HostShell.SelectedItem = item;

        chrome.Status.Message = "Idle";
        return window;
    }
}
