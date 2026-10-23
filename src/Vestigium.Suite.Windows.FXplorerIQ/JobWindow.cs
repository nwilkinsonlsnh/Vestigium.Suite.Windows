        var body = new StackPanel();
        body.Children.Add(_phase);
        body.Children.Add(Path("Source", source));
        if (!string.IsNullOrWhiteSpace(destination))
            body.Children.Add(Path("Destination", destination));
        body.Children.Add(_bar);
        body.Children.Add(_rate);
        body.Children.Add(bins);

        var action = new DockPanel { Margin = new Thickness(4) };
        DockPanel.SetDock(actions, Dock.Bottom);
        DockPanel.SetDock(body, Dock.Top);
        action.Children.Add(actions);
        action.Children.Add(body);
        action.Children.Add(scroller);

        var actionTab = new TabItem { Header = "Action", Content = action };