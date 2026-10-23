        if (verb == JobVerb.Copy)
        {
            _ = Copy(source, destination!, destPane, verify);
            return;
        }