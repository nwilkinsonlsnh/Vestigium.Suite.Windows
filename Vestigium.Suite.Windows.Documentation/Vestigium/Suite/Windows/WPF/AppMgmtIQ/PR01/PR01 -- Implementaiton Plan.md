# AppMgmtIQ — PR01 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PLAN-PR01  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**APPID:** `AppMgmtIQ`  
**Status:** Live  
**Date:** 16 October 2026  
**Binding:** Requirements v1.0 wins on the window. Design v1.0 wins on types. `Vestigium.Helpers.WinReg` 1.0.0 wins on the inventory. This file wins on the slice.

**Goal:** Ship the first usable AppMgmtIQ window: suite chrome, grid of every package from the four ARP roots, single selection, double-click details window that surfaces every registry value in organized tabs plus computed isX64 and isGlobal.

**Not:** An uninstaller. Not a store-app viewer. Not winget. Not a second registry walk inside the host. Not bulk. Not repair/modify.

---

## Starting point (repo as of this plan)

The project exists. It is an empty WinExe with package references.

| Piece | Today | Required |
|---|---|---|
| `App.OnStartup` | Missing | HostLog → Themes → DI → window. |
| Window | Missing | `AppMgmtIqWindow` + `VestigiumShell`. Grid + details. |
| ViewModel | Missing | Inventory VM. Single selection. Double-click command. |
| Details | Missing | Multi-tab window. Title = DisplayName (DisplayVersion). |
| Tests | Empty project | Host tests that do not open the registry. |

---

## Decision

One PR. Four build slices plus owner gate.

| Call | Why |
|---|---|
| Thin host | Suite rule. WinReg owns the walk and the model. |
| Single selection | Requirements lock. |
| Details on double-click | Requirements lock. |
| All four roots | Requirements lock. |
| No uninstall control | Explicitly out. Do not leave a dead button. |
| Host tests stay off the registry | Protocol/inventory tests live in Helpers. |

### Rejected alternatives

| Idea | Why out |
|---|---|
| Host walks the registry itself | Forbidden façade. |
| Multi-select “for later” | Requirements say one package at a time. |
| Uninstall button that does nothing | Dead control is fog. |
| Store apps in the same grid | Different store. Different PR. |

---

## Library doors this slice may call

Pin: `Vestigium.Helpers.WinReg` **1.0.0**.

The host calls the public inventory surface that returns the package list and the full value set for a selected package. The host does not open `RegistryKey` itself.

Computed fields isX64 and isGlobal are supplied by the Helper.

---

## Types to land

| Type | File | Role |
|---|---|
| `App` | `App.xaml.cs` | Startup order. |
| `AppMgmtIqWindow` | `AppMgmtIqWindow.xaml` / `.cs` | Chrome + grid. DataContext only. |
| `MainViewModel` | `ViewModels/MainViewModel.cs` | Inventory, selection, double-click command. |
| `PackageRow` | `ViewModels/PackageRow.cs` | Grid projection. |
| `DetailsWindow` | `DetailsWindow.xaml` / `.cs` | Multi-tab details. Title bound. |
| `DetailsViewModel` | `ViewModels/DetailsViewModel.cs` | Values organized into tabs. |

No `IAppMgmtService`. Static Helper surface stays the door.

---

## Window map

`AppMgmtIqWindow` + `VestigiumShell`

```
menu  File (Exit)
      View (bar visible, Theme submenu)
icon  Assets/AppMgmtIQ.ico
nav   (single page this release)
client
  grid  DisplayName, Publisher, DisplayVersion, InstallDate, EstimatedSize, isX64, isGlobal, UninstallString
status bar
```

Double-click a row → `DetailsWindow`.

Details title: `{DisplayName} ({DisplayVersion})`.

Details content: tabs that group the raw registry values (General, Uninstall, Links, Raw, etc.). isX64 and isGlobal visible.

---

## Behavior

1. On startup, ViewModel asks the Helper for the full inventory.
2. Grid binds to the collection. Selection mode = Single.
3. Double-click: open (or refresh) the details window with the selected package’s full value set.
4. Title updates with the selection.
5. No uninstall path. No second walk.

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR01-01 | Chrome skeleton. `AppMgmtIqWindow`, ThemeCatalog, ThemeChrome, package pins, icon. | Open |
| 2 | PR01-02 | Inventory grid. Bind to Helper. Single selection. | Open |
| 3 | PR01-03 | Details window. Double-click. Title. Tabs for all values. | Open |
| 4 | PR01-04 | Host tests. No registry. | Open |
| 5 | PR01-05 | Owner runs the window against Requirements. | Owner |

---

## Files this plan expects to touch

```
src/Vestigium.Suite.Windows.AppMgmtIQ/App.xaml
src/Vestigium.Suite.Windows.AppMgmtIQ/App.xaml.cs
src/Vestigium.Suite.Windows.AppMgmtIQ/AppMgmtIqWindow.xaml          [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/AppMgmtIqWindow.xaml.cs       [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/ThemeCatalog.cs               [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/ThemeChrome.cs                [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/Assets/AppMgmtIQ.ico          [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/MainViewModel.cs   [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/PackageRow.cs      [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/DetailsWindow.xaml            [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/DetailsWindow.xaml.cs         [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/DetailsViewModel.cs [NEW]
tests/Vestigium.Suite.Windows.Tests/HostIdsTests.cs                [NEW]
```

Do not add a project reference to Helpers. Package only.

When this plan finishes, move the whole `PR01/` folder to `Complete/PR01/`.

---

## What each watch

| Role | Watch |
|---|---|
| Alvin | No façade. No uninstall button. Grid columns match the Helper model. |
| Theodore | Details window does not throw on missing values. Single selection is enforced. Tests stay off the registry. |
| Simon | isX64 / isGlobal come from the Helper, not a host guess. All four roots are included. |

---

## Next action

PR01-01. Chrome skeleton. Inventory and details stay stubs until the next slices.
