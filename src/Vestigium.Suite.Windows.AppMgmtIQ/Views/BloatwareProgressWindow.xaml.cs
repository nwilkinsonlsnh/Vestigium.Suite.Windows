using System.Windows;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class BloatwareProgressWindow : Window
{
    public BloatwareProgressWindow(string action, string name)
    {
        InitializeComponent();
        Title = action + " Bloatware";
        Heading.Text = Title;
        NameText.Text = name;
        StatusText.Text = action == "Remove" ? "Removing the package…" : "Looking for a local package, then fetching it if needed…";
    }

    public void Complete(string outcome, string detail)
    {
        Bar.Visibility = Visibility.Collapsed;
        StatusText.Text = outcome;
        DetailBox.Text = string.IsNullOrWhiteSpace(detail) ? "No additional detail." : detail;
        DetailBox.Visibility = Visibility.Visible;
        CopyButton.Visibility = Visibility.Visible;
        CloseButton.Visibility = Visibility.Visible;
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
        => Clipboard.SetText(Heading.Text + "\n" + StatusText.Text + "\n" + NameText.Text + "\n" + DetailBox.Text);
}
