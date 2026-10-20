# AppMgmtIQ — PR04 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PLAN-PR04  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**APPID:** `AppMgmtIQ`  
**Status:** Draft  
**Date:** 19 October 2026  
**Binding:** PR04 Requirements wins on behavior. This file wins on the slice.

**Goal:** Add Repair/Modify launch, orphan detection, and simple version baseline comparison to the existing package inventory. Extend the Package KQL catalog so the new fields are queryable through the existing QueryBar. Update the Dashboard Packages section (or add a Lifecycle section) with cards and charts for orphans, action availability, and baseline status.

**Not:** An auto-repair engine. Not a deployment system. Not a new QueryBar.

---

## Starting point

Inventory grid and uninstall flow exist. ModifyPath and QuietUninstallString are already read in some projections. The packages view already has a `VestigiumQueryBar` bound to `AppMgmtKql.Packages()`. Dashboard Packages section currently shows counts, publisher mix, install year, and size only.

| Piece | Today | Required |
|---|---|---|
| Modify / Repair actions | Missing | Confirmed launch when path exists. |
| Orphan indicator | Missing | Detect missing install location. |
| Version baseline | Missing | Simple expected-version comparison. |
| Pending reboot | Missing | Surface when readable. |
| Package catalog fields | Missing the new signals | Add IsOrphan, HasModify, HasRepair, BaselineStatus, PendingReboot. |
| QueryBar | Present | Keep. New fields become queryable. |
| Dashboard lifecycle signals | Missing | Cards/charts for orphans, Repair/Modify availability, baseline match/newer/older. |

---

## Decision

| Call | Why |
|---|---|
| Reuse uninstall confirmation pattern | Consistent safety. |
| Orphan = indicator only | No auto-cleanup. |
| Baseline is local/simple | Lab-friendly. |
| Extend existing Package catalog | QueryBar is already on the packages view. Requirements R04-05. |
| Extend existing Packages Dashboard section | Avoid a new radio for data that is a property of packages. Optional Lifecycle radio only if the section becomes crowded. Requirements R04-06. |

---

## Types to land

| Type | File | Role |
|---|---|
| updates to `PackageCatalog` | `Vestigium.Helpers.Kql.AppMgmt/PackageCatalog.cs` | New fields. |
| updates to `PackageFacts` / `PackageAdapter` | same project | Project the new signals. |
| updates to `PackageRow` | `ViewModels/PackageRow.cs` | Orphan flag, baseline status, action flags. |
| `BaselineStore` | `ViewModels/BaselineStore.cs` | Persist simple expected versions. |
| context menu / details updates | existing views | Repair / Modify commands. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR04.001 | Surface ModifyPath and build Repair command when product code is known. | Done |
| 2 | PR04.002 | Add confirmed Modify and Repair actions (reuse uninstall confirmation). | Done |
| 3 | PR04.003 | Orphan detection (missing InstallLocation or key file). Indicator in grid and details. | Done |
| 4 | PR04.004 | Simple version baseline store and comparison column. | Done |
| 5 | PR04.005 | Pending reboot / file-rename surface (read-only). | Done |
| 6 | PR04.006 | Extend PackageCatalog, PackageFacts, and PackageAdapter with the new fields from R04-05. Existing QueryBar picks them up through the existing session. | Done |
| 7 | PR04.007 | Host tests for orphan logic, baseline comparison, and new catalog field binding (no live registry). | Done |
| 8 | PR04.008 | Dashboard: extend the Packages section (or add a Lifecycle radio if preferred at implementation time). Cards: orphans, packages with Repair available, packages with Modify available, baseline match / newer / older / none. Charts: orphan vs clean pie, baseline status breakdown, action availability. Story text updated to mention health. | Done |
| 9 | PR04.009 | Owner runs against Requirements. | Owner |

---

## Files this plan expects to touch

```
# Vestigium.Helpers
src/Vestigium.Helpers.Kql.AppMgmt/PackageCatalog.cs         [UPDATE]
src/Vestigium.Helpers.Kql.AppMgmt/PackageFacts.cs           [UPDATE]
src/Vestigium.Helpers.Kql.AppMgmt/PackageAdapter.cs         [UPDATE]

# Vestigium.Suite.Windows
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/PackageRow.cs          [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/MainViewModel.cs       [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/BaselineStore.cs       [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/AppMgmtIqView.xaml          [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/AppMgmtIqView.xaml.cs       [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/DetailsWindow.xaml                [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/DashboardView.xaml         [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/DashboardView.xaml.cs      [UPDATE]
```

When this plan finishes, move the whole `PR04/` folder to `Complete/PR04/`.

---

## Next action

PR04.009. Owner runs against Requirements.
