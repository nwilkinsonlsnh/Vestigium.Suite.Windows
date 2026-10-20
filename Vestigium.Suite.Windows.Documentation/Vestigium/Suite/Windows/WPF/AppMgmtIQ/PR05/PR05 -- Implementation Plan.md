# AppMgmtIQ — PR05 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PLAN-PR05
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`
**APPID:** `AppMgmtIQ`
**Status:** Draft
**Date:** 20 October 2026
**Binding:** PR05 Requirements wins on behavior. This file wins on the slice.

**Goal:** Move domain work out of the host into published Helpers. The host keeps the views, the bindings, the confirmation, and the elevated appliers. The libraries do the read, the resolve, the rules, and the plans.

**Not:** A behavior change. Not a new tab. Not a cleanup action.

---

## Starting point

The host owns path resolution, orphan rules, repair and uninstall plans, baseline compare, the inventory walks, startup classification, the bloatware catalog, and the pending-reboot read. Helpers already cover registry reads (`WinReg`), the package query catalog (`Kql.AppMgmt`), workbook writing (`ClosedXml`), and machine info (`SystemInfo`).

| Piece | Today | Required |
|---|---|---|
| Path rules | Host: `PathResolve`, `AppPathResolve` | `Vestigium.Helpers.FileSystemPaths` |
| Wow64 / WinSxS | Host: `FootprintsViewModel` | Same package |
| Orphan, repair, uninstall, baseline compare | Host | `Vestigium.Helpers.AppMgmt` |
| Inventory walks | Host: `MainViewModel`, `FootprintsViewModel` | Same package |
| Startup rules | Host: `StartupApproved`, `StartupLaunch`, `StartupLink`, `StartupNavigation` | `Vestigium.Helpers.Startup` |
| Bloatware catalog | Host: `BloatwareCatalog` | `Vestigium.Helpers.Bloatware` |
| Pending reboot | Host: `PendingReboot` | `Vestigium.Helpers.SystemInfo` |

---

## Decision

| Call | Why |
|---|---|
| One package per domain | Matches the existing Helper layout. |
| FileSystemPaths is new | Not WinReg, not FileIo. |
| Applier stays in the host | Elevated process, host manifest. |
| Features and DISM parked | Different domain. |
| Each slice publishes and pins | Host builds against the published package. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR05.001 | Create `Vestigium.Helpers.FileSystemPaths`. Move `PathResolve` and `AppPathResolve`. Host calls it. Publish and pin. | |
| 2 | PR05.002 | Move the Wow64 retry and the WinSxS name search into FileSystemPaths. | |
| 3 | PR05.003 | Create `Vestigium.Helpers.AppMgmt`. Move orphan rules, repair plan, uninstall plan, and baseline compare. The baseline store stays in the host. | |
| 4 | PR05.004 | Move the inventory walks (ARP, App Paths, SharedDLLs, products, associations) and the footprint link into AppMgmt. Host projects rows only. | |
| 5 | PR05.005 | Create `Vestigium.Helpers.Startup`. Move approved, launch, link, and navigation. Applier stays in the host. | |
| 6 | PR05.006 | Create `Vestigium.Helpers.Bloatware`. Move `BloatwareCatalog`. | |
| 7 | PR05.007 | Move the pending-reboot read to `Vestigium.Helpers.SystemInfo`. | |
| 8 | PR05.008 | Owner runs. Export matches the PR04 export for Footprints Exists, package counts, and startup rows. | Owner |

---

## Files this plan expects to touch

```
# Vestigium.Helpers (new)
src/Vestigium.Helpers.FileSystemPaths/                       [NEW]
src/Vestigium.Helpers.AppMgmt/                               [NEW]
src/Vestigium.Helpers.Startup/                               [NEW]
src/Vestigium.Helpers.Bloatware/                             [NEW]

# Vestigium.Helpers (existing)
src/Vestigium.Helpers.SystemInfo/                            [UPDATE]

# Vestigium.Suite.Windows
src/Vestigium.Suite.Windows.AppMgmtIQ/PathResolve.cs                    [DELETE]
src/Vestigium.Suite.Windows.AppMgmtIQ/AppPathResolve.cs                 [DELETE]
src/Vestigium.Suite.Windows.AppMgmtIQ/OrphanRules.cs                    [DELETE]
src/Vestigium.Suite.Windows.AppMgmtIQ/RepairPlan.cs                     [DELETE]
src/Vestigium.Suite.Windows.AppMgmtIQ/UninstallPlan.cs                  [DELETE]
src/Vestigium.Suite.Windows.AppMgmtIQ/BaselineCompare.cs                [DELETE]
src/Vestigium.Suite.Windows.AppMgmtIQ/PendingReboot.cs                  [DELETE]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/MainViewModel.cs       [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/FootprintsViewModel.cs [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/StartupViewModel.cs    [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/BloatwareCatalog.cs    [DELETE]
Directory.Build.props                                                   [UPDATE]
```

When this plan finishes, move the whole `PR05/` folder to `Completed/PR05/`.

---

## Next action

PR05.001. Create `Vestigium.Helpers.FileSystemPaths` and move the path rules.
