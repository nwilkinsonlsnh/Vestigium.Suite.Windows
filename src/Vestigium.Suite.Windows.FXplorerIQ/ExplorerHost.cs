        menu.Opened += (_, _) => RefreshMenu();
        ContextMenu = menu;
        PreviewMouseRightButtonUp += OpenMenu;