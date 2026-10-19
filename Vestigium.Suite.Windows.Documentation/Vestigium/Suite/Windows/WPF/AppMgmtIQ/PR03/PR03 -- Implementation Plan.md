# AppMgmtIQ — PR03 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PLAN-PR03  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**APPID:** `AppMgmtIQ`  
**Status:** Draft  
**Date:** 19 October 2026  
**Binding:** PR03 Requirements wins on behavior. This file wins on the slice.

**Goal:** Add a read-only Footprints view covering App Paths, installer-linked associations, Windows Installer product info, and SharedDLLs.

**Not:** A registry editor. Not full COM. Not environment editing.

---

## Starting point

PR02 (Startup) may or may not be complete. Footprints is independent.

| Piece | Today | Required |
|---|---|---|
| Footprints tab | Missing | New tab with sections. |
| App Paths | Missing | Enumerate three locations. |
| Associations | Missing | Installer-linked ProgIDs / extensions only. |
| Installer products | Missing | Product codes when present. |
| SharedDLLs | Missing | Name + ref count. |

---

## Decision

| Call | Why |
|---|---|
| One tab, multiple sections or filter | Avoid tab explosion. |
| Temporary host walk acceptable | Until Helper doors exist. |
| Read-only | Requirements lock. |

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
| 8 | PR03.008 | Owner runs against Requirements. | Owner |

---

## Files this plan expects to touch

```
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/FootprintRow.cs         [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/FootprintsViewModel.cs  [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/FootprintsView.xaml          [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/FootprintsView.xaml.cs       [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/AppMgmtIqWindow.xaml               [UPDATE]
```

When this plan finishes, move the whole `PR03/` folder to `Complete/PR03/`.

---

## Next action

PR03.001. Add the Footprints tab shell.
