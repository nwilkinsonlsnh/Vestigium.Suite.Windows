using System.Windows;
using System.Windows.Controls;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class SnapshotView : UserControl
{
    private bool _loading;

    public SnapshotView()
    {
        InitializeComponent();
        Loaded += (_, _) => Load();
    }

    private void Load()
    {
        _loading = true;
        var choice = SnapshotChoice.Load();
        Packages.IsChecked = choice.Packages;
        Features.IsChecked = choice.Features;
        BuiltIn.IsChecked = choice.BuiltIn;
        Setup.IsChecked = choice.ActiveSetup;
        Startup.IsChecked = choice.Startup;
        Footprints.IsChecked = choice.Footprints;
        _loading = false;
    }

    private void Choice_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading)
            return;
        SnapshotChoice.Save(new SnapshotChoice
        {
            Packages = Packages.IsChecked == true,
            Features = Features.IsChecked == true,
            BuiltIn = BuiltIn.IsChecked == true,
            ActiveSetup = Setup.IsChecked == true,
            Startup = Startup.IsChecked == true,
            Footprints = Footprints.IsChecked == true
        });
    }
}
