        if (verb == JobVerb.Mirror)
        {
            _ = Mirror(source, destination!, destPane);
            return;
        }