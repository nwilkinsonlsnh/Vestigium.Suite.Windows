    public void Start(Action<string> status)
    {
        _status = status;
        if (_explorers.Count > 0)
            return;

        var records = _session.Load().Explorers;
        if (records.Count == 0)
        {
            AddExplorer();
            return;
        }

        for (var i = 0; i < records.Count; i++)
            AddExplorer(records[i].Path, records[i].Name, select: i == 0, split: records[i].Split, second: records[i].SecondPath);
    }

    private void SaveSession()
    {
        var session = new ExplorerSession();
        foreach (var slot in _explorers)
            session.Explorers.Add(new ExplorerRecord
            {
                Name = slot.Name,
                Path = slot.Pane.Folder,
                Split = slot.View.IsSplit,
                SecondPath = slot.View.Second.Folder
            });
        _session.Save(session);
    }