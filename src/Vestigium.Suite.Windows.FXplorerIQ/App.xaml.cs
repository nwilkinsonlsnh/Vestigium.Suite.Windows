using System.Windows;
using Vestigium.Themes;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public partial class App : Application
{
    public static ThemeManager Themes { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        HostLog.Initialize(HostIds.FXplorerIQ);
        Themes.RegisterSuiteV1();
        Themes.Initialize(this, "LightBlue");
        base.OnStartup(e);
    }
}
