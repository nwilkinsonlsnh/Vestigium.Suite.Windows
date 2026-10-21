# AppMgmtIQ — PR06 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PLAN-PR06
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`
**APPID:** `AppMgmtIQ`
**Status:** Draft
**Date:** 20 October 2026
**Binding:** PR06 Requirements wins on behavior. This file wins on the slice.

**Goal:** A baseline is a named snapshot of all six inventory tabs. Capture it, compare a later read, and show the drift.

**Not:** A cleanup. Not an automatic action.

---

## Starting point

`BaselineStore` saves one expected version per package key in `baselines.json`. Nothing in the views calls `SetBaseline`. The Dashboard Baseline chart therefore reads None for every package.

| Piece | Today | Required |
|---|---|---|
| Store | One version per package | One file per snapshot, six inventories |
| Capture | No UI | One action on the Dashboard |
| Compare | Version only | The field each inventory cares about |
| Chart | None (501) | match, changed, added, removed, none |
| Grid | No baseline column | A Baseline column on each inventory tab |

---

## Decision

| Call | Why |
|---|---|
| One file per snapshot | A snapshot is a moment. Files are easier to select and delete than one growing document. |
| Capture what is loaded | No second scan. The grids are the source. |
| Identity is stable | Hive and key, feature name, family, component id, scope and key, kind and path. |
| Retire `baselines.json` | It cannot represent the other tabs. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR06.001 | New snapshot store. One file per capture, six inventories, named with machine and time. | Done |
| 2 | PR06.002 | Capture action on the Dashboard. Writes all six from the loaded grids. | Done |
| 3 | PR06.003 | Compare. match, changed, added, removed, none. Packages by version. Features by enabled. Built-In by installed and provisioned. Active Setup by version and installed. Startup by enabled and command. Footprints by exists. | |
| 4 | PR06.004 | Dashboard Baseline chart splits by the compare result for the selected inventory. | |
| 5 | PR06.005 | Baseline column on each inventory grid. | |
| 6 | PR06.006 | Select a snapshot and delete a snapshot. | |
| 7 | PR06.007 | Retire `baselines.json` and the per-package `SetBaseline`. | |
| 8 | PR06.008 | Owner runs. Capture, change one row, compare, see the drift. | Owner |

---

## Files this plan expects to touch

```
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/BaselineStore.cs        [REPLACE]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/MainViewModel.cs        [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/FeaturesViewModel.cs    [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/BloatwareViewModel.cs   [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/ActiveSetupViewModel.cs [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/StartupViewModel.cs     [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/FootprintsViewModel.cs  [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/DashboardView.xaml           [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/DashboardView.xaml.cs        [UPDATE]
```

When this plan finishes, move the whole `PR06/` folder to `Completed/PR06/`.

---

## Next action

PR06.003. Compare.
