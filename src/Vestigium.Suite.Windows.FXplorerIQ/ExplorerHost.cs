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