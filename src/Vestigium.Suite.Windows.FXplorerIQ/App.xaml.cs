        HostLog.Initialize(HostIds.FXplorerIQ);
        HostLog.Information(FXplorerCatalog.AppStarted, "Lifecycle", "FXplorerIQ opened.");
        Themes.RegisterSuiteV1();
        Themes.Initialize(this, SavedTheme());