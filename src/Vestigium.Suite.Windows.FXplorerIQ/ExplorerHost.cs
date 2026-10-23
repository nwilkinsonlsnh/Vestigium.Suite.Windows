        if (verb == JobVerb.Delete)
        {
            _ = Delete(source, sourcePane, verify);
            return;
        }

        _status("Mirror (Verify) is ready. " + source);