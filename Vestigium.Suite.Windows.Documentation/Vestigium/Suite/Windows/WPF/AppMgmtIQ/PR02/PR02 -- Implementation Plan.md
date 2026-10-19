# AppMgmtIQ — PR02 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PLAN-PR02  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**APPID:** `AppMgmtIQ`  
**Status:** Draft  
**Date:** 19 October 2026  
**Binding:** PR02 Requirements wins on behavior. Design v1.0 wins on types. WinReg (or temporary host walk) wins on the data. This file wins on the slice.

**Goal:** Add a read-only Startup / Persistence view that lists Run, RunOnce, RunOnceEx, and Startup folder entries and links them to installed packages when possible.

**Not:** A startup cleaner. Not Scheduled Tasks. Not services. Not any write path.

---

## Starting point (repo as of this plan)

AppMgmtIQ has inventory, features, bloatware, and Active Setup tabs. No startup view.

| Piece | Today | Required |
|---|---|---|
| Startup tab | Missing | New tab with grid. |
| Run enumeration | Missing | HKLM/HKCU, both views, Run/RunOnce/RunOnceEx. |
| Startup folders | Missing | User and common Startup folders. |
| Package link | Missing | Best-effort match to existing package inventory. |
| Tests | Host tests exist | New tests stay off live registry. |

---

## Decision

One PR. Build slices plus owner gate.

| Call | Why |
|---|---|
| Read-only | Requirements lock. |
| Best-effort linkage | Useful without perfect matching. |
| Temporary host walk acceptable | Until a Helper door owns Run enumeration. |
| No write controls | Explicitly out. |

### Rejected alternatives

| Idea | Why out |
|---|---|
| Disable toggle | Out of release. |
| Scheduled Tasks | Different PR. |
| Host opens RegistryKey directly for long term | Prefer Helper door later. Temporary is allowed for this slice. |

---

## Library doors this slice may call

Pin: `Vestigium.Helpers.WinReg` (current).  
Pin: existing package inventory from MainViewModel.

**Debt:** Run enumeration may live in the host until a WinReg or new Helper door owns it.

---

## Types to land

| Type | File | Role |
|---|---|
| `StartupRow` | `ViewModels/StartupRow.cs` | Grid projection. |
| `StartupViewModel` | `ViewModels/StartupViewModel.cs` | Load, filter, package link. |
| `StartupView` | `Views/StartupView.xaml` / `.cs` | Tab content. |
| (optional) `StartupDetails` | inline or small window | Raw command + source path. |

---

## Window map

New tab **Startup** in main navigation.

```
grid  Name, Command, Source, Scope, Bit, Package
status  count of entries
```

Selection shows command and source path. No action buttons that modify state.

---

## Behavior

1. On tab load (or app start), enumerate all listed locations.
2. Build StartupRow collection.
3. For each row, attempt package match against current inventory.
4. Grid binds. Single selection.
5. No writes.

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR02.001 | Add Startup tab shell and empty ViewModel. Navigation only. | |
| 2 | PR02.002 | Enumerate Run / RunOnce / RunOnceEx (HKLM + HKCU, both views). Project to StartupRow. | |
| 3 | PR02.003 | Enumerate user and common Startup folders. Shortcuts resolved to targets where possible. | |
| 4 | PR02.004 | Best-effort package linkage against existing inventory. | |
| 5 | PR02.005 | Grid columns, selection, status count. Theme the grid. | |
| 6 | PR02.006 | Host tests for row projection and linkage logic (no live registry). | |
| 7 | PR02.007 | Owner runs against Requirements. | Owner |

---

## Files this plan expects to touch

```
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/StartupRow.cs          [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/StartupViewModel.cs   [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/StartupView.xaml           [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/StartupView.xaml.cs        [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/AppMgmtIqWindow.xaml             [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/App.xaml.cs                      [UPDATE]
```

When this plan finishes, move the whole `PR02/` folder to `Complete/PR02/`.

---

## What each watch

| Role | Watch |
|---|---|
| Alvin | No write paths. No façade that bypasses Helper long-term. |
| Theodore | Missing keys or empty folders do not throw. Grid stays themed. |
| Simon | Both views and both Startup folders are included. Package link is best-effort only. |

---

## Next action

PR02.001. Add the Startup tab shell.
