using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class ExplorerHost : DockPanel
{
    private readonly Grid _body = new();
    private readonly GridSplitter _split = new();
    private readonly Button _splitButton;
    private readonly Button _verticalButton;
    private readonly Button _horizontalButton;
    private bool _applying;

    public ExplorerHost(Action<string> status)
    {
        LastChildFill = true;
        First = new ExplorerPane { Status = status };
        Second = new ExplorerPane { Status = status };
        First.FillCell = true;
        Second.FillCell = true;

        var bar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(12, 8, 12, 0)
        };
        _splitButton = Command("Split", "Open a second pane", Split_Click);
        _verticalButton = Command("Vertical", "Side by side", () => SetVertical(true));
        _horizontalButton = Command("Horizontal", "Stacked", () => SetVertical(false));
        bar.Children.Add(_splitButton);
        bar.Children.Add(_verticalButton);
        bar.Children.Add(_horizontalButton);
        Children.Add(bar);
        DockPanel.SetDock(bar, Dock.Top);

        _split.Width = 8;
        _split.HorizontalAlignment = HorizontalAlignment.Stretch;
        _split.VerticalAlignment = VerticalAlignment.Stretch;
        _split.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
        _split.ShowsPreview = false;
        _split.DragCompleted += Split_DragCompleted;
        Children.Add(_body);

        Apply(false, App.Settings.Load());
    }

    public ExplorerPane First { get; }

    public ExplorerPane Second { get; }

    public bool IsSplit { get; private set; }

    public bool Vertical { get; private set; } = true;

    public double Ratio { get; private set; } = 0.5;

    public event Action? Changed;

    public void Apply(bool split, FXplorerSettings settings, string? secondPath = null)
    {
        _applying = true;
        try
        {
            Vertical = settings.SplitVertical;
            Ratio = settings.SplitRatio is > 0.1 and < 0.9 ? settings.SplitRatio : 0.5;
            IsSplit = split;
            if (!string.IsNullOrWhiteSpace(secondPath))
                Second.Seed(secondPath);
            Layout();
        }
        finally
        {
            _applying = false;
        }
    }

    private void Split_Click()
    {
        IsSplit = !IsSplit;
        if (IsSplit && string.IsNullOrWhiteSpace(Second.Folder))
            Second.Seed(First.Folder);
        Layout();
        Save();
        HostLog.Information(IsSplit ? FXplorerCatalog.SplitOpened : FXplorerCatalog.SplitClosed, "Lifecycle", IsSplit ? "Split opened." : "Split closed.");
        Changed?.Invoke();
    }

    private void SetVertical(bool vertical)
    {
        if (!IsSplit || Vertical == vertical)
            return;
        Vertical = vertical;
        Layout();
        Save();
        Changed?.Invoke();
    }

    private void Split_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        var total = Vertical ? _body.ActualWidth : _body.ActualHeight;
        if (total < 100)
            return;
        var first = Vertical ? _body.ColumnDefinitions[0].ActualWidth : _body.RowDefinitions[0].ActualHeight;
        var ratio = first / total;
        if (ratio is < 0.15 or > 0.85)
            return;
        Ratio = Math.Round(ratio, 3);
        Save();
        Changed?.Invoke();
    }

    private void Layout()
    {
        _body.Children.Clear();
        _body.RowDefinitions.Clear();
        _body.ColumnDefinitions.Clear();

        var first = new GridLength(Ratio, GridUnitType.Star);
        var second = new GridLength(1 - Ratio, GridUnitType.Star);
        if (Vertical)
        {
            _body.ColumnDefinitions.Add(new ColumnDefinition { Width = first, MinWidth = 240 });
            _body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            _body.ColumnDefinitions.Add(new ColumnDefinition { Width = second, MinWidth = 240 });
            _split.ResizeDirection = GridResizeDirection.Columns;
            _split.Width = 8;
            _split.Height = double.NaN;
            Grid.SetColumn(First, 0);
            Grid.SetRow(First, 0);
            Grid.SetColumn(_split, 1);
            Grid.SetRow(_split, 0);
            Grid.SetColumn(Second, 2);
            Grid.SetRow(Second, 0);
        }
        else
        {
            _body.RowDefinitions.Add(new RowDefinition { Height = first, MinHeight = 180 });
            _body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            _body.RowDefinitions.Add(new RowDefinition { Height = second, MinHeight = 180 });
            _split.ResizeDirection = GridResizeDirection.Rows;
            _split.Height = 8;
            _split.Width = double.NaN;
            Grid.SetRow(First, 0);
            Grid.SetColumn(First, 0);
            Grid.SetRow(_split, 1);
            Grid.SetColumn(_split, 0);
            Grid.SetRow(Second, 2);
            Grid.SetColumn(Second, 0);
        }

        _body.Children.Add(First);
        if (IsSplit)
        {
            _body.Children.Add(_split);
            _body.Children.Add(Second);
            if (string.IsNullOrWhiteSpace(Second.Folder))
                Second.Open(First.Folder);
        }

        _splitButton.Content = IsSplit ? "Close split" : "Split";
        _verticalButton.Visibility = IsSplit ? Visibility.Visible : Visibility.Collapsed;
        _horizontalButton.Visibility = IsSplit ? Visibility.Visible : Visibility.Collapsed;
        _verticalButton.Opacity = Vertical ? 1 : 0.55;
        _horizontalButton.Opacity = Vertical ? 0.55 : 1;
    }

    private void Save()
    {
        if (_applying)
            return;
        var settings = App.Settings.Load();
        settings.SplitVertical = Vertical;
        settings.SplitRatio = Ratio;
        App.Settings.Save(settings);
    }

    private Button Command(string text, string tip, Action click)
    {
        var button = new Button
        {
            Content = text,
            ToolTip = tip,
            Style = (Style)Application.Current.FindResource("Button.Utility"),
            MinWidth = 88,
            MinHeight = 32,
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(12, 4, 12, 4)
        };
        button.Click += (_, _) => click();
        return button;
    }
}
