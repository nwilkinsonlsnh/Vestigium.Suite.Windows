        if (verb == JobVerb.Copy)
        {
            _ = Copy(source, destination!, destPane, verify);
            return;
        }

        if (verb == JobVerb.Move && !verify)
        {
            _ = Move(source, destination!, sourcePane, destPane);
            return;
        }