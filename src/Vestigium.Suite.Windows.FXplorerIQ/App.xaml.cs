        var pane = new ExplorerPane { Status = text => chrome.Status.Message = text };
        Set(window.HostShell, "Explorer", pane);
        Set(window.HostShell, "Dashboard", new VestigiumUnderConstruction