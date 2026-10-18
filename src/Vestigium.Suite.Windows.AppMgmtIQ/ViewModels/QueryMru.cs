using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class QueryMruRow : ObservableObject
{
    [ObservableProperty]
    private string _text = "";

    [ObservableProperty]
    private bool _isSticky;
}

public static class QueryMru
{
    public static void Record(ObservableCollection<QueryMruRow> list, string text, int limit, int stickyLimit)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text) || limit <= 0)
            return;

        var existing = list.FirstOrDefault(row => string.Equals(row.Text, text, StringComparison.OrdinalIgnoreCase));
        if (existing?.IsSticky == true)
            return;

        if (existing is not null)
            list.Remove(existing);

        var stickies = list.Count(row => row.IsSticky);
        list.Insert(Math.Min(stickies, list.Count), new QueryMruRow { Text = text, IsSticky = false });
        Trim(list, limit);
    }

    public static bool ToggleSticky(ObservableCollection<QueryMruRow> list, QueryMruRow row, int stickyLimit)
    {
        if (row.IsSticky)
        {
            row.IsSticky = false;
            list.Remove(row);
            var stickies = list.Count(row => row.IsSticky);
            list.Insert(Math.Min(stickies, list.Count), row);
            return true;
        }

        if (list.Count(row => row.IsSticky) >= Math.Max(0, stickyLimit))
            return false;

        row.IsSticky = true;
        list.Remove(row);
        var count = list.Count(row => row.IsSticky);
        list.Insert(Math.Min(count, list.Count), row);
        return true;
    }

    public static void Move(ObservableCollection<QueryMruRow> list, QueryMruRow row, int delta)
    {
        var index = list.IndexOf(row);
        if (index < 0)
            return;
        var next = index + delta;
        if (next < 0 || next >= list.Count)
            return;
        if (list[next].IsSticky != row.IsSticky)
            return;
        list.Move(index, next);
    }

    public static void Trim(ObservableCollection<QueryMruRow> list, int limit)
    {
        while (list.Count > limit)
        {
            var last = list.LastOrDefault(row => !row.IsSticky);
            if (last is null)
                break;
            list.Remove(last);
        }
    }
}
