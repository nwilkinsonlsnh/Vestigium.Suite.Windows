    private FileIoJobResult? ShowJob(string title, FileIoJob job)
    {
        var window = new JobWindow(title, job) { Owner = Window.GetWindow(this) };
        window.ShowDialog();
        return window.Result;
    }