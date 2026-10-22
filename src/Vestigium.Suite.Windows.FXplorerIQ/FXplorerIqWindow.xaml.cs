        pane.Navigated += _ => RefreshLabel(slot);
        host.Children.Add(tab);
        host.Children.Add(close);
        _explorers.Add(slot);
        ExplorerTabs.Children.Add(host);
        RefreshClose();
        HostLog.Information(FXplorerCatalog.ExplorerOpened, "Lifecycle", "Explorer opened. " + (string.IsNullOrWhiteSpace(path) ? "New." : path));
        if (select)
            tab.IsChecked = true;
        else
            RefreshLabel(slot);
        FitTabs();