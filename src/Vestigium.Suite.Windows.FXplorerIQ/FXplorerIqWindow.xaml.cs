using System.Windows;
using System.Windows.Controls;
using Vestigium.Controls.Shell;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public partial class FXplorerIqWindow : Window
{
    public FXplorerIqWindow(VestigiumDefaultWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        viewModel.RootShell = RootShell;
        SourceInitialized += OnSourceInitialized;
    }

    public VestigiumDefaultWindowViewModel ViewModel { get; }

    public VestigiumShell HostShell => RootShell;

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var work = SystemParameters.WorkArea;
        var width = Math.Min(1800, Math.Max(1200, work.Width - 48));
        var height = Math.Min(980, Math.Max(640, work.Height - 48));
        Width = width;
        Height = height;
        Left = work.Left + Math.Max(0, (work.Width - Width) / 2);
        Top = work.Top + Math.Max(0, (work.Height - Height) / 2);
    }

    private void MainNav_Checked(object sender, RoutedEventArgs e)
    {
        if (RootShell is null || sender is not RadioButton { Content: string header })
            return;

        var item = RootShell[header];
        if (item is not null)
            RootShell.SelectedItem = item;
    }
}
