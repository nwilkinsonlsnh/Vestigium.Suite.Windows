    private readonly ExplorerSessionStore _session = new(FXplorerSettingsStore.DefaultRoot);

    public FXplorerIqWindow(VestigiumDefaultWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        viewModel.RootShell = RootShell;
        SourceInitialized += OnSourceInitialized;
        Closing += (_, _) => SaveSession();
    }