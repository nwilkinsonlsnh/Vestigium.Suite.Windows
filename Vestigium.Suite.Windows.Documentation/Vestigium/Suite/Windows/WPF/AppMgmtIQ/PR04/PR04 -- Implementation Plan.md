# AppMgmtIQ — PR04 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PLAN-PR04  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**APPID:** `AppMgmtIQ`  
**Status:** Draft  
**Date:** 19 October 2026  
**Binding:** PR04 Requirements wins on behavior. This file wins on the slice.

**Goal:** Add Repair/Modify launch, orphan detection, and simple version baseline comparison to the existing package inventory.

**Not:** An auto-repair engine. Not a deployment system.

---

## Starting point

Inventory grid and uninstall flow exist. ModifyPath and QuietUninstallString are already read in some projections.

| Piece | Today | Required |
|---|---|---|
| Modify / Repair actions | Missing | Confirmed launch when path exists. |
| Orphan indicator | Missing | Detect missing install location. |
| Version baseline | Missing | Simple expected-version comparison. |
| Pending reboot | Missing | Surface when readable. |

---

## Decision

| Call | Why |
|---|---|
| Reuse uninstall confirmation pattern | Consistent safety. |
| Orphan = indicator only | No auto-cleanup. |
| Baseline is local/simple | Lab-friendly. |

---

## Types to land

| Type | File | Role |
|---|---|
| updates to `PackageRow` | `ViewModels/PackageRow.cs` | Orphan flag, baseline status. |
| `BaselineStore` | `ViewModels/BaselineStore.cs` | Persist simple expected versions. |
| context menu / details updates | existing views | Repair / Modify commands. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR04.001 | Surface ModifyPath and build Repair command when product code is known. | |
| 2 | PR04.002 | Add confirmed Modify and Repair actions (reuse uninstall confirmation). | |
| 3 | PR04.003 | Orphan detection (missing InstallLocation or key file). Indicator in grid and details. | |
| 4 | PR04.004 | Simple version baseline store and comparison column. | |
| 5 | PR04.005 | Pending reboot / file-rename surface (read-only). | |
| 6 | PR04.006 | Host tests for orphan logic and baseline comparison (no live registry). | |
| 7 | PR04.007 | Owner runs against Requirements. | Owner |

---

## Files this plan expects to touch

```
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/PackageRow.cs          [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/MainViewModel.cs       [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/BaselineStore.cs       [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/AppMgmtIqView.xaml          [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/AppMgmtIqView.xaml.cs       [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/DetailsWindow.xaml                [UPDATE]
```

When this plan finishes, move the whole `PR04/` folder to `Complete/PR04/`.

---

## Next action

PR04.001. Surface ModifyPath and Repair command construction.
