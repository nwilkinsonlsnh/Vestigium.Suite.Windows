namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class MainViewModel
{
    public IReadOnlyList<PackageRow> AllPackages => _all;
}

public sealed partial class FeaturesViewModel
{
    public IReadOnlyList<FeatureRow> AllFeatures => _all;

    public IEnumerable<FeatureNode> VisibleNodes()
    {
        foreach (var root in Roots)
            foreach (var node in Walk(root))
                yield return node;
    }

    public string ParentOf(string name) =>
        _parents.TryGetValue(name, out var parent) ? parent : "";

    private static IEnumerable<FeatureNode> Walk(FeatureNode node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var nested in Walk(child))
                yield return nested;
    }
}
