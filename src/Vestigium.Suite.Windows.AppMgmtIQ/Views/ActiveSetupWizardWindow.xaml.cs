using System.Windows;
using Microsoft.Win32;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class ActiveSetupWizardWindow : Window
{
    public ActiveSetupWizardWindow()
    {
        InitializeComponent();
        ComponentBox.Text = "{" + Guid.NewGuid().ToString().ToUpperInvariant() + "}";
    }

    public bool Accepted { get; private set; }
    public string DisplayName => NameBox.Text.Trim();
    public string Component => ComponentBox.Text.Trim();
    public string StubPath => StubBox.Text.Trim();
    public string Version => string.IsNullOrWhiteSpace(VersionBox.Text) ? "1,0" : VersionBox.Text.Trim();
    public bool IsGlobal => MachineBox.IsChecked == true;
    public bool IsX64 => X64Box.IsChecked == true;
    private void Generate_Click(object sender, RoutedEventArgs e)
        => ComponentBox.Text = "{" + Guid.NewGuid().ToString().ToUpperInvariant() + "}";

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose the Active Setup Command",
            Filter = "Programs and Scripts|*.exe;*.bat;*.cmd;*.ps1;*.dll|All Files|*.*"
        };
        if (dialog.ShowDialog(this) == true)
            StubBox.Text = dialog.FileName;
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (DisplayName.Length == 0 || Component.Length == 0)
        {
            ErrorText.Text = "Display name and component are required.";
            return;
        }
        Accepted = true;
        DialogResult = true;
    }
}
