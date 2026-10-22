using System.Windows.Controls;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class ExplorerSlot
{
    public required string Id { get; init; }

    public required string Header { get; set; }

    public string? Name { get; set; }

    public required ExplorerPane Pane { get; init; }

    public required RadioButton Tab { get; init; }

    public required Button Close { get; init; }

    public required StackPanel Host { get; init; }
}
