using System.Windows;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class UninstallConfirmWindow : Window
{
    public UninstallConfirmWindow(string name, string command, string actor, string? note, string verb = "Uninstall")
    {
        InitializeComponent();
        Title = verb;
        Heading.Text = verb;
        ConfirmButton.Content = verb;
        NameText.Text = $"{verb} {name}?";
        CommandBox.Text = string.IsNullOrWhiteSpace(note) ? command : command + "\n\n" + note;
        ActorText.Text = "Actor: " + actor;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
