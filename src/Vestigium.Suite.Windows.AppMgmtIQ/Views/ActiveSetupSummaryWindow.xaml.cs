using System.Windows;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class ActiveSetupSummaryWindow : Window
{
    public ActiveSetupSummaryWindow(string heading, string outcome, string detail, string error)
    {
        InitializeComponent();
        Heading.Text = heading;
        Outcome.Text = outcome;
        Detail.Text = detail;
        ErrorBox.Text = string.IsNullOrWhiteSpace(error) ? "No error." : error;
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
        => Clipboard.SetText(Heading.Text + "\n" + Outcome.Text + "\n" + Detail.Text + "\n" + ErrorBox.Text);
}
