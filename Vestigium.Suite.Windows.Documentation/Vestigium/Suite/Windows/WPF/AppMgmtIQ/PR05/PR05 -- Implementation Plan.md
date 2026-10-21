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

The host owns path resolution, install health, repair and uninstall plans, baseline compare, the inventory walks, startup classification, the bloatware catalog, and the pending-reboot read. Helpers already cover registry reads (`WinReg`), the package query catalog (`Kql.AppMgmt`), workbook writing (`ClosedXml`), and machine info (`SystemInfo`).

| Piece | Today | Required |
|---|---|---|
| Path rules | Host: `PathResolve`, `AppPathResolve` | `Vestigium.Helpers.FileSystemPaths` |
| Sanitize, tokens, long paths | Missing | Same package |
| Wow64 / WinSxS | Host: `FootprintsViewModel` | Same package |
| Install health, repair, uninstall | Host, takes `PackageRow` | Stays in the host |
| Inventory walks | Host: `MainViewModel`, `FootprintsViewModel` | Stays in the host |
| Baseline compare | Host: `BaselineCompare` | `Vestigium.Helpers.SystemInfo` |
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
| 1 | PR05.001 | Create `Vestigium.Helpers.FileSystemPaths`. Move `PathResolve` and `AppPathResolve`. Host calls it. Publish and pin. | Done |
| 2 | PR05.002 | Move the Wow64 retry and the WinSxS name search into FileSystemPaths. | Done |
| 2b | PR05.002b | Add `PathSanitize`. Rejected characters become one safe character. Never empty. | Done |
| 2c | PR05.002c | Add the token expander. Categories: date and time (`{datetime}` `{date}` `{time}`), special folders (desktop, favorites, userprofile, appdata, localappdata, temp, programfiles, programfilesx86, programdata, windir, systemdrive, public), identity (`{username}` `{computername}`), counters (`{#}` `{##}` `{A}` `{AAA}` `{A#}` `{AA##}` `{a}` `{aaa}`, any width). Number runs zero-pad and throw when the value does not fit. Letter runs are base-26, rightmost first (`AAA`, `AAB`, `AAZ`, `ABA`), and throw past the last value. The two parts count separately. No rollover. Unknown tokens stay as written. | Done |
| 2d | PR05.002d | Long paths, conservative. Prefix `\\\\?\\` only when a built path would exceed 260. Leave short paths and already-long paths alone. | Done |
| 2e | PR05.002e | `PathCheck.IsValid`. A `{...}` pair must be a known token. `{{` and `}}` are literal braces. Overflow and illegal path characters are invalid. Does not check that the file exists. | Done |
| 3 | PR05.003 | Move `BaselineCompare` to `Vestigium.Helpers.SystemInfo`. The baseline store stays in the host. Install health, repair, uninstall, and the inventory walks stay in the host. | |
| 4 | PR05.004 | Create `Vestigium.Helpers.Startup`. Move approved, launch, link, and navigation. Applier stays in the host. | |
| 5 | PR05.005 | Create `Vestigium.Helpers.Bloatware`. Move `BloatwareCatalog`. | |
| 6 | PR05.006 | Move the pending-reboot read to `Vestigium.Helpers.SystemInfo`. | |
| 7 | PR05.007 | Owner runs. Export matches the PR04 export for Footprints Exists, package counts, and startup rows. | Owner |

---

## Files this plan expects to touch

```
# Vestigium.Helpers (new)
src/Vestigium.Helpers.FileSystemPaths/                       [NEW]
src/Vestigium.Helpers.Startup/                               [NEW]
src/Vestigium.Helpers.Bloatware/                             [NEW]

# Vestigium.Helpers (existing)
src/Vestigium.Helpers.SystemInfo/                            [UPDATE]

# Vestigium.Suite.Windows
src/Vestigium.Suite.Windows.AppMgmtIQ/PathResolve.cs                    [DELETE]
src/Vestigium.Suite.Windows.AppMgmtIQ/AppPathResolve.cs                 [DELETE]
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

PR05.003. Move BaselineCompare to SystemInfo.
