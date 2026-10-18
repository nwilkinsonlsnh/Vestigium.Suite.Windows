using System.Windows;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class BloatwareFetchWindow : Window
{
    public BloatwareFetchWindow(string name, bool provisioned)
    {
        InitializeComponent();
        Body.Text = provisioned
            ? $"{name} is provisioned on this machine. Register installs it for you. Get from Store fetches a fresh copy."
            : $"{name} is not provisioned on this machine. There is nothing local to register. Get from Store opens the Microsoft Store page.";
        Detail.Text = name;
    }

    public bool OpenStore { get; private set; }
    public bool RegisterLocal { get; private set; }

    private void Store_Click(object sender, RoutedEventArgs e)
    {
        OpenStore = true;
        DialogResult = true;
    }

    private void Register_Click(object sender, RoutedEventArgs e)
    {
        RegisterLocal = true;
        DialogResult = true;
    }
}
