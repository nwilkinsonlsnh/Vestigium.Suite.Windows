# Vestigium.Suite.Windows

WPF hosts for the Vestigium Windows suite. Not the helper libraries. Those stay in [Vestigium.Helpers](https://github.com/nwilkinsonlsnh/Vestigium.Helpers).

## Stack

| Item | Value |
|---|---|
| IDE | Visual Studio 2026 |
| Language | C# |
| TFM | `net10.0-windows` (.NET 10 LTS) |
| UI | WPF |
| Architecture | MVVM (`CommunityToolkit.Mvvm`) |
| Solution | `Vestigium.Suite.Windows.slnx` |

## Projects

| Project | Kind | Owns |
|---|---|---|
| `AppMgmtIQ` | exe | Thin shell. Installed-app inventory and uninstall. Logic in Helpers. |

Open `Vestigium.Suite.Windows.slnx` in Visual Studio 2026.

## Rules

- No project reference to `Vestigium.Helpers`. Package only.
- Host is chrome only. Registry walk, model, and uninstall launch live in the Helper.
