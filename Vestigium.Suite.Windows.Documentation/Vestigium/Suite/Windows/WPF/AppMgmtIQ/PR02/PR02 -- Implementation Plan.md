# AppMgmtIQ — PR02 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PLAN-PR02  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**APPID:** `AppMgmtIQ`  
**Status:** Draft  
**Date:** 19 October 2026  
**Binding:** PR02 Requirements wins on behavior. Design v1.0 wins on types. WinReg (or temporary host walk) wins on the data. This file wins on the slice.

**Goal:** Add a Startup / Persistence view that lists Run, RunOnce, RunOnceEx, and Startup folder entries, links them to installed packages, filters with a Startup KQL catalog, summarizes them on the Dashboard, and lets an administrator enable or disable entries by writing the Windows StartupApproved flag. HKCU writes stay in the host. HKLM and common writes go through `Vestigium.Suite.Windows.AppMgmtIQ.Startup`.

**Not:** A Scheduled Task editor. Not a service controller. Not AutorunsDisabled. Not a private cache of deleted values. Not an elevated main application.

---

## Starting point (repo as of this plan)

AppMgmtIQ has inventory, features, bloatware, and Active Setup tabs, each with a QueryBar and a KQL catalog in `Vestigium.Helpers.Kql.*`. Elevated write hosts already exist: `AppMgmtIQ.Features`, `AppMgmtIQ.Bloatware`, `AppMgmtIQ.ActiveSetup`. Each is a WinExe with `requireAdministrator`, launched with `Verb = runas` and a job file.

| Piece | Today | Required |
|---|---|---|
| Startup tab | Missing | New tab with grid. |
| Run enumeration | Missing | HKLM/HKCU, both views, Run/RunOnce/RunOnceEx. |
| Startup folders | Missing | User and common Startup folders. |
| Package link | Missing | Best-effort match to existing package inventory. |
| Startup KQL catalog | Missing | Catalog + session in Helpers.Kql.AppMgmt. |
| QueryBar on Startup | Missing | Same wiring as existing tabs. |
| Dashboard Startup section | Missing | Cards and charts for source, scope, state, and linkage. |
| Enable / Disable | Missing | StartupApproved write. One and all in the current view. |
| Elevated write host | Missing | `AppMgmtIQ.Startup`, same shape as Features. |
| Tests | Host tests exist | New tests stay off live registry. |

---

## Decision

One PR. Inventory slices land first. Write slices follow, then the owner gate.

| Call | Why |
|---|---|
| StartupApproved for disable | Windows flag. Other tools agree. Entry stays visible. |
| AutorunsDisabled out | Tool convention. Windows does not read it. |
| Bulk on the filtered view | Streamlines administration. A filter is the scope. |
| Confirm on bulk | Count and scope split before the write. |
| Separate elevated project | Same pattern as Features and Bloatware. Main app stays unelevated. |
| HKCU in process | No UAC for a user-scoped write. |
| One UAC per elevated batch | Not one prompt per row. |
| RunOnce skipped for disable | No flag. Remove only. |
| Temporary host walk acceptable | Until a Helper door owns Run enumeration. |
| Catalog in Helpers.Kql.AppMgmt | Same home as Packages / Features / Bloatware. |
| Dashboard section in this PR | Avoid lag between data and summary. |

### Rejected alternatives

| Idea | Why out |
|---|---|
| AutorunsDisabled subkey | Not a Windows flag. Hides the entry. |
| Delete as disable | Not reversible by other tools. |
| Silent Disable All | Operator must see the count. |
| Elevate the main application | Elevates reads and UI that do not need it. |
| UAC per row | Noisy. One launch per batch. |
| Scheduled Tasks | Different PR. |
| Host opens RegistryKey directly for long term | Prefer Helper door later. Temporary is allowed. |
| Host-local field list | Breaks the shared KQL pattern. |

---

## Library doors this slice may call

Pin: `Vestigium.Helpers.WinReg` (current).  
Pin: `Vestigium.Helpers.Kql` and `Vestigium.Helpers.Kql.AppMgmt` (catalog + session).  
Pin: `Vestigium.Controls.QueryBar` (control).  
Pin: existing package inventory from MainViewModel.  
Pin: existing ChartView / ChartOptions from Vestigium.Helpers.Charts.

**Debt:** Run enumeration and the StartupApproved write may live in the host and the elevated exe until a WinReg or new Helper door owns them.

---

## Types to land

| Type | File | Role |
|---|---|
| `StartupCatalog` | `Vestigium.Helpers.Kql.AppMgmt/StartupCatalog.cs` | Field list. |
| `StartupFacts` | `Vestigium.Helpers.Kql.AppMgmt/StartupFacts.cs` | Projection input. |
| `StartupAdapter` | `Vestigium.Helpers.Kql.AppMgmt/StartupAdapter.cs` | Facts to KqlRow. |
| `AppMgmtKql.Startup()` | `Vestigium.Helpers.Kql.AppMgmt/AppMgmtKql.cs` | Session factory. |
| `StartupRow` | `ViewModels/StartupRow.cs` | Grid projection. |
| `StartupViewModel` | `ViewModels/StartupViewModel.cs` | Load, filter, package link, Query/Commit/Pin, enable/disable. |
| `StartupView` | `Views/StartupView.xaml` / `.cs` | Tab content + QueryBar + context menu. |
| `StartupApproved` | `ViewModels/StartupApproved.cs` | Flag bytes. |
| `StartupJob` / `StartupResult` | `AppMgmtIQ.Startup` | Job and result for the elevated exe. |
| `AppMgmtIQ.Startup` | `src/Vestigium.Suite.Windows.AppMgmtIQ.Startup` | Elevated WinExe. `requireAdministrator`. |

---

## Window map

New tab **Startup** in main navigation.

```
QueryBar  (FilterRequested / Committed / PinRequested)
grid  Name, Command, Source, Scope, Bit, State, Package
context menu  Enable, Disable, Enable All, Disable All, Remove, Open source, Jump to package
status  count of entries
```

Selection shows command, source path, and state. Dashboard gains a **Startup** radio section with cards and charts driven by the same data.

Elevated project has no shell of its own. It reads a job, applies the writes, writes a result, and exits. Same shape as `AppMgmtIQ.Features`.

---

## Behavior

1. On tab load (or app start), enumerate all listed locations.
2. Build StartupRow collection.
3. For each row, attempt package match against current inventory.
4. Grid binds. Single selection.
5. QueryBar live-filters via `ApplyQuery`. Commit records MRU only when the query binds.
6. StartupApproved companion keys mark each Run and Startup folder entry Enabled or Disabled. Missing flag means Enabled. `startup.enabled` is searchable.
7. Enable and Disable write the StartupApproved flag. Confirm is not required for one row.
8. Enable All and Disable All act on the current filtered rows. Confirm shows the count and the HKCU / HKLM split. RunOnce and RunOnceEx are skipped.
9. HKCU writes happen in the host. HKLM and common writes are written to a job file and applied by `AppMgmtIQ.Startup.exe`, launched with `runas`.
10. A mixed bulk action does HKCU in process and HKLM in one elevated launch. Cancelling UAC reports the HKLM items as cancelled.
11. Dashboard Startup section reads the same collection and renders summary cards and charts.

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR02.001 | Add Startup tab shell and empty ViewModel. Navigation only. | Done |
| 2 | PR02.002 | Enumerate Run / RunOnce / RunOnceEx (HKLM + HKCU, both views). Project to StartupRow. | Done |
| 3 | PR02.003 | Enumerate user and common Startup folders. Shortcuts resolved to targets where possible. | Done |
| 4 | PR02.004 | Best-effort package linkage against existing inventory. | Done |
| 5 | PR02.005 | Grid columns, selection, status count. Theme the grid. | Done |
| 6 | PR02.006 | KQL catalog: add StartupCatalog, StartupFacts, StartupAdapter, and AppMgmtKql.Startup() in Vestigium.Helpers.Kql.AppMgmt. Fields match R02-05. | Done |
| 7 | PR02.007 | Wire VestigiumQueryBar in StartupView (same bindings and events as AppMgmtIqView). ViewModel implements ApplyQuery / CommitQuery / Pin / QueryMru. | Done |
| 8 | PR02.008 | Host tests for row projection, linkage logic, and catalog field binding (no live registry). | Done |
| 9 | PR02.009 | Dashboard: add Startup radio section. Bind to StartupViewModel. Cards and charts. Story text updated. | Done |
| 10 | PR02.011 | Read StartupApproved. Show Enabled/Disabled. Searchable as startup.enabled. Dashboard counts. | Done |
| 11 | PR02.012 | Context menu shell: Enable, Disable, Enable All, Disable All, Remove, Open source, Jump to package. Commands no-op except Copy. | Done |
| 12 | PR02.013 | StartupApproved write for one HKCU row. In process. Log. Refresh the row. | Done |
| 13 | PR02.014 | Create `Vestigium.Suite.Windows.AppMgmtIQ.Startup`. WinExe, `requireAdministrator`, job/result contract matching Features. | Done |
| 14 | PR02.015 | Host launches `AppMgmtIQ.Startup` for HKLM and common writes. One launch per batch. Cancelled UAC is a status. | Done |
| 15 | PR02.016 | Enable All and Disable All on the current filtered rows. Confirm with count and scope split. Skip RunOnce / RunOnceEx. Split HKCU in process, HKLM through the elevated exe. | Done |
| 16 | PR02.017 | Remove for registry values and Startup folder items. Confirm. Same elevation split. | |
| 17 | PR02.018 | Open source and Jump to package. | |
| 18 | PR02.010 | Owner runs against Requirements. | Owner |

---

## Files this plan expects to touch

```
# Vestigium.Helpers
src/Vestigium.Helpers.Kql.AppMgmt/StartupCatalog.cs           [NEW]
src/Vestigium.Helpers.Kql.AppMgmt/StartupFacts.cs             [NEW]
src/Vestigium.Helpers.Kql.AppMgmt/StartupAdapter.cs           [NEW]
src/Vestigium.Helpers.Kql.AppMgmt/AppMgmtKql.cs               [UPDATE]

# Vestigium.Suite.Windows
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/StartupRow.cs          [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/StartupViewModel.cs   [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/ViewModels/StartupApproved.cs    [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/StartupView.xaml           [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/StartupView.xaml.cs        [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/AppMgmtIqWindow.xaml             [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/App.xaml.cs                      [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/DashboardView.xaml         [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/DashboardView.xaml.cs      [UPDATE]

src/Vestigium.Suite.Windows.AppMgmtIQ.Startup/                        [NEW]
  Vestigium.Suite.Windows.AppMgmtIQ.Startup.csproj
  app.manifest                          requireAdministrator
  StartupJob.cs                         job and result
  App.xaml.cs                           read job, apply, write result
```

When this plan finishes, move the whole `PR02/` folder to `Complete/PR02/`.

---

## What each watch

| Role | Watch |
|---|---|
| Alvin | Writes go through StartupApproved, not a private key. Main app is not elevated. Catalog lands in Helpers. |
| Theodore | Missing keys do not throw. Cancelled UAC is a status. Grid stays themed. |
| Simon | Bulk acts on the filtered view only. Confirm shows the count. One UAC per elevated batch. RunOnce is skipped. |

---

## Next action

PR02.017. Remove for registry values and Startup folder items.
