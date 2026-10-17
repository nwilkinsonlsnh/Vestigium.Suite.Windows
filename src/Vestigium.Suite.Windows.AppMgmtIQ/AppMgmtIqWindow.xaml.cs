using System.Windows;
using Vestigium.Controls.Shell;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public partial class AppMgmtIqWindow : Window
{
    public AppMgmtIqWindow(VestigiumDefaultWindowViewModel viewModel)
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
        var width = Math.Min(1400, Math.Max(1000, work.Width - 48));
        var height = Math.Min(900, Math.Max(600, work.Height - 48));
        Width = width;
        Height = height;
        Left = work.Left + Math.Max(0, (work.Width - Width) / 2);
        Top = work.Top + Math.Max(0, (work.Height - Height) / 2);
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
