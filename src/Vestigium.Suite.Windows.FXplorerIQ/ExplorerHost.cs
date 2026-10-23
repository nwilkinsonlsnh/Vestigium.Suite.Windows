    private void Verb(JobVerb verb, bool verify)
    {
        if (_busy)
        {
            _status("A job is already running.");
            return;
        }

        var sourcePane = SourceIsFirst ? First : Second;
        var destPane = SourceIsFirst ? Second : First;
        var source = sourcePane.SelectionOrFolder();
        string? destination = null;

        if (verb != JobVerb.Delete)
        {
            var suggested = IsSplit && !string.IsNullOrWhiteSpace(destPane.Folder) ? destPane.Folder : null;
            var picked = JobConfirm.Ask(Window.GetWindow(this), Label(verb, verify), source, suggested);
            if (picked is null)
            {
                _status("Cancelled.");
                return;
            }

            destination = picked;
        }

        var guard = JobGuard.Check(verb, source, destination);
        if (!guard.Ok)
        {
            _status(guard.Reason);
            return;
        }

        if (!JobAccess.CanRun(verb, source, destination))
        {
            var elevated = JobHandoff.Offer(verb, source, destination, verify && verb == JobVerb.Delete, false);
            _status(elevated ? "Elevated job started." : "Elevation cancelled.");
            return;
        }

        if (verb == JobVerb.Copy)
        {
            _ = Copy(source, destination!, destPane, verify);
            return;
        }

        if (verb == JobVerb.Move)
        {
            _ = Move(source, destination!, sourcePane, destPane, verify);
            return;
        }

        if (verb == JobVerb.Delete)
        {
            _ = Delete(source, sourcePane, verify);
            return;
        }

        _ = Mirror(source, destination!, destPane);
    }

    private void Audit_Click()
    {
        if (_busy)
        {
            _status("A job is already running.");
            return;
        }

        var sourcePane = SourceIsFirst ? First : Second;
        var destPane = SourceIsFirst ? Second : First;
        var source = sourcePane.SelectionOrFolder();
        var suggested = IsSplit && !string.IsNullOrWhiteSpace(destPane.Folder) ? destPane.Folder : null;
        var destination = JobConfirm.Ask(Window.GetWindow(this), "Audit", source, suggested);
        if (destination is null)
        {
            _status("Cancelled.");
            return;
        }

        var guard = JobGuard.Check(JobVerb.Copy, source, destination);
        if (!guard.Ok)
        {
            _status(guard.Reason);
            return;
        }

        if (!JobAccess.CanRun(JobVerb.Copy, source, destination))
        {
            var elevated = JobHandoff.Offer(JobVerb.Copy, source, destination, false, true);
            _status(elevated ? "Elevated audit started." : "Elevation cancelled.");
            return;
        }

        _ = Audit(source, destination);
    }

    private static string Label(JobVerb verb, bool verify)
        => verb switch
        {
            JobVerb.Copy => verify ? "Copy (Verify)" : "Copy",
            JobVerb.Move => verify ? "Move (Verify)" : "Move",
            JobVerb.Mirror => "Mirror (Verify)",
            _ => verb.ToString()
        };