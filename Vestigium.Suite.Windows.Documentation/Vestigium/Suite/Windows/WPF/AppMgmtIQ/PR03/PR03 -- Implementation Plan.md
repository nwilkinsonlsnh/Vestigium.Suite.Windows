# AppMgmtIQ — PR03 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PLAN-PR03  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**APPID:** `AppMgmtIQ`  
**Status:** Draft  
**Date:** 19 October 2026  
**Binding:** PR03 Requirements wins on behavior. This file wins on the slice.

**Goal:** Add a read-only Footprints view covering App Paths, installer-linked associations, Windows Installer product info, and SharedDLLs. Update the Dashboard with a Footprints section that summarizes the new data.

**Not:** A registry editor. Not full COM. Not environment editing.

---

## Starting point

PR02 (Startup) may or may not be complete. Footprints is independent. Dashboard currently covers Packages, Features, Bloatware, and Active Setup (and Startup once PR02 lands).

| Piece | Today | Required |
|---|---|---|
| Footprints tab | Missing | New tab with sections. |
| App Paths | Missing | Enumerate three locations. |
| Associations | Missing | Installer-linked ProgIDs / extensions only. |
| Installer products | Missing | Product codes when present. |
| SharedDLLs | Missing | Name + ref count. |
| Dashboard Footprints section | Missing | Cards and charts for App Paths, SharedDLLs, and installer products. |

---

## Decision

| Call | Why |
|---|---|
| One tab, multiple sections or filter | Avoid tab explosion. |
| Temporary host walk acceptable | Until Helper doors exist. |
| Read-only | Requirements lock. |
| Dashboard section in this PR | Keep summary aligned with the new tab. |

---

## Types to land

| Type | File | Role |
|---|---|
| `FootprintRow` | `ViewModels/FootprintRow.cs` | Common projection. |
| `FootprintsViewModel` | `ViewModels/FootprintsViewModel.cs` | Load sections. |
| `FootprintsView` | `Views/FootprintsView.xaml` / `.cs` | Tab content. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR03.001 | Add Footprints tab shell and empty ViewModel. | |
| 2 | PR03.002 | Enumerate App Paths (HKLM, Wow, HKCU). Project to FootprintRow. | |
| 3 | PR03.003 | Enumerate SharedDLLs with reference counts. | |
| 4 | PR03.004 | Surface Windows Installer product codes when present. | |
| 5 | PR03.005 | Installer-linked associations / ProgIDs (best-effort). | |
| 6 | PR03.006 | Package linkage and themed grid. | |
| 7 | PR03.007 | Host tests for projection (no live registry). | |
| 8 | PR03.008 | Dashboard: add Footprints radio section. Bind to FootprintsViewModel. Cards: App Paths count, SharedDLLs with refcount > 1, installer products found, total footprint rows. Charts: breakdown by type (App Paths / SharedDLLs / Products / Associations), SharedDLL refcount distribution or top shared, product presence. Story text updated. | |
| 9 | PR03.009 | Owner runs against Requirements. | Owner |

---

## Files this plan expects to touch

```
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/FootprintRow.cs         [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/FootprintsViewModel.cs  [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/FootprintsView.xaml          [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/FootprintsView.xaml.cs       [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/AppMgmtIqWindow.xaml               [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/DashboardView.xaml           [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/DashboardView.xaml.cs        [UPDATE]
```

When this plan finishes, move the whole `PR03/` folder to `Complete/PR03/`.

---

## Next action

PR03.001. Add the Footprints tab shell.
