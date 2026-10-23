        menu.Items.Add(Item("Mirror (Verify)", () => Verb(JobVerb.Mirror, true)));
        menu.Items.Add(Item("Audit", Audit_Click));
        menu.Items.Add(new Separator());