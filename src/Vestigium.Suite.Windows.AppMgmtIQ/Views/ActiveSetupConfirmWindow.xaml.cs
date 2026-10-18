using System.Windows;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class ActiveSetupConfirmWindow : Window
{
    public ActiveSetupConfirmWindow(string name, string path)
    {
        InitializeComponent();
        NameText.Text = $"Delete {name}? The key is removed. New users will not run it.";
        PathText.Text = path;
    }

    public bool Accepted { get; private set; }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        Accepted = true;
        DialogResult = true;
    }
}
