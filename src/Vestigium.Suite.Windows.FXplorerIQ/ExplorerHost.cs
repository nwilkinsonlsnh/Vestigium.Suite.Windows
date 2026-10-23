        if (verb == JobVerb.Delete && !verify)
        {
            _ = Delete(source, sourcePane);
            return;
        }