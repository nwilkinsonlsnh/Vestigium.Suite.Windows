        if (verb == JobVerb.Move)
        {
            _ = Move(source, destination!, sourcePane, destPane, verify);
            return;
        }