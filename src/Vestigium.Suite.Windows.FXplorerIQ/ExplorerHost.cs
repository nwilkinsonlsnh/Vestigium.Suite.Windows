    private FileIoJobResult? ShowJob(string title, FileIoJob job, string source, string? destination)
    {
        JobLog.Start(title, job.JobId, source, destination);
        var window = new JobWindow(title, job) { Owner = Window.GetWindow(this) };
        window.ShowDialog();
        JobLog.End(title, job.JobId, source, destination, window.Result?.Status ?? "Failed");
        return window.Result;
    }