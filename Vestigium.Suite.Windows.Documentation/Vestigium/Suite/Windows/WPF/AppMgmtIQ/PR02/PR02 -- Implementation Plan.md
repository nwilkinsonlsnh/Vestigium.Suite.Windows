# AppMgmtIQ — PR02 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PLAN-PR02  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**APPID:** `AppMgmtIQ`  
**Status:** Draft  
**Date:** 19 October 2026  
**Binding:** PR02 Requirements wins on behavior. Design v1.0 wins on types. WinReg (or temporary host walk) wins on the data. This file wins on the slice.

**Goal:** Add a read-only Startup / Persistence view that lists Run, RunOnce, RunOnceEx, and Startup folder entries and links them to installed packages when possible. Add a Startup KQL catalog and wire the QueryBar for filtering. Update the Dashboard with a Startup section that summarizes the new data.

**Not:** A startup cleaner. Not Scheduled Tasks. Not services. Not any write path.

---

## Starting point (repo as of this plan)

AppMgmtIQ has inventory, features, bloatware, and Active Setup tabs, each with a QueryBar and a KQL catalog in `Vestigium.Helpers.Kql.*`. No startup view. Dashboard has sections for Packages, Features, Bloatware, and Active Setup only.

| Piece | Today | Required |
|---|---|---|
| Startup tab | Missing | New tab with grid. |
| Run enumeration | Missing | HKLM/HKCU, both views, Run/RunOnce/RunOnceEx. |
| Startup folders | Missing | User and common Startup folders. |
| Package link | Missing | Best-effort match to existing package inventory. |
| Startup KQL catalog | Missing | Catalog + session in Helpers.Kql.AppMgmt (or sibling). |
| QueryBar on Startup | Missing | Same wiring as existing tabs. |
| Dashboard Startup section | Missing | Cards and charts for source, scope, and linkage. |
| Tests | Host tests exist | New tests stay off live registry. |

---

## Decision

One PR. Build slices plus owner gate. Catalog, QueryBar, and Dashboard are part of this PR so the new tab matches the existing filter and summary experience.

| Call | Why |
|---|---|
| Read-only | Requirements lock. |
| Best-effort linkage | Useful without perfect matching. |
| Temporary host walk acceptable | Until a Helper door owns Run enumeration. |
| No write controls | Explicitly out. |
| Catalog in Helpers.Kql.AppMgmt | Same home as Packages / Features / Bloatware. Sibling project only if the domain grows. |
| QueryBar + catalog in this PR | Requirements R02-05. |
| Dashboard section in this PR | Avoid lag between data and summary. Requirements R02-06. |

### Rejected alternatives

| Idea | Why out |
|---|---|
| Disable toggle | Out of release. |
| Scheduled Tasks | Different PR. |
| Host opens RegistryKey directly for long term | Prefer Helper door later. Temporary is allowed for this slice. |
| Dedicated Dashboard PR later | Dashboard would lag the data. |
| Host-local field list | Breaks the shared KQL pattern. |

---

## Library doors this slice may call

Pin: `Vestigium.Helpers.WinReg` (current).  
Pin: `Vestigium.Helpers.Kql` and `Vestigium.Helpers.Kql.AppMgmt` (catalog + session).  
Pin: `Vestigium.Controls.QueryBar` (control).  
Pin: existing package inventory from MainViewModel.  
Pin: existing ChartView / ChartOptions from Vestigium.Helpers.Charts.

**Debt:** Run enumeration may live in the host until a WinReg or new Helper door owns it. Catalog and adapter land in Helpers.

---

## Types to land

| Type | File | Role |
|---|---|
| `StartupCatalog` | `Vestigium.Helpers.Kql.AppMgmt/StartupCatalog.cs` | Field list. |
| `StartupFacts` | `Vestigium.Helpers.Kql.AppMgmt/StartupFacts.cs` | Projection input. |
| `StartupAdapter` | `Vestigium.Helpers.Kql.AppMgmt/StartupAdapter.cs` | Facts to KqlRow. |
| `AppMgmtKql.Startup()` | `Vestigium.Helpers.Kql.AppMgmt/AppMgmtKql.cs` | Session factory. |
| `StartupRow` | `ViewModels/StartupRow.cs` | Grid projection. |
| `StartupViewModel` | `ViewModels/StartupViewModel.cs` | Load, filter, package link, Query/Commit/Pin. |
| `StartupView` | `Views/StartupView.xaml` / `.cs` | Tab content + QueryBar. |
| (optional) `StartupDetails` | inline or small window | Raw command + source path. |

---

## Window map

New tab **Startup** in main navigation.

```
QueryBar  (FilterRequested / Committed / PinRequested)
grid  Name, Command, Source, Scope, Bit, Package
status  count of entries
```

Selection shows command and source path. No action buttons that modify state.

Dashboard gains a **Startup** radio section (alongside Packages / Features / Bloatware / Active Setup) with cards and charts driven by the same StartupViewModel data.

---

## Behavior

1. On tab load (or app start), enumerate all listed locations.
2. Build StartupRow collection.
3. For each row, attempt package match against current inventory.
4. Grid binds. Single selection.
5. QueryBar live-filters via `ApplyQuery`. Commit records MRU only when the query binds.
6. No writes.
7. Dashboard Startup section reads the same collection and renders summary cards and charts.

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR02.001 | Add Startup tab shell and empty ViewModel. Navigation only. | Done |
| 2 | PR02.002 | Enumerate Run / RunOnce / RunOnceEx (HKLM + HKCU, both views). Project to StartupRow. | |
| 3 | PR02.003 | Enumerate user and common Startup folders. Shortcuts resolved to targets where possible. | |
| 4 | PR02.004 | Best-effort package linkage against existing inventory. | |
| 5 | PR02.005 | Grid columns, selection, status count. Theme the grid. | |
| 6 | PR02.006 | KQL catalog: add StartupCatalog, StartupFacts, StartupAdapter, and AppMgmtKql.Startup() in Vestigium.Helpers.Kql.AppMgmt. Fields match R02-05. | |
| 7 | PR02.007 | Wire VestigiumQueryBar in StartupView (same bindings and events as AppMgmtIqView). ViewModel implements ApplyQuery / CommitQuery / Pin / QueryMru. | |
| 8 | PR02.008 | Host tests for row projection, linkage logic, and catalog field binding (no live registry). | |
| 9 | PR02.009 | Dashboard: add Startup radio section. Bind to StartupViewModel. Cards: total entries, by source (Run / RunOnce / Startup folder), scope (HKLM vs HKCU), linked vs unlinked. Charts: source breakdown (Pareto or pie), scope pie, linked vs unlinked. Story text updated. | |
| 10 | PR02.010 | Owner runs against Requirements. | Owner |

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
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/StartupView.xaml           [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/StartupView.xaml.cs        [NEW]
src/Vestigium.Suite.Windows.AppMgmtIQ/AppMgmtIqWindow.xaml             [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/App.xaml.cs                      [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/DashboardView.xaml         [UPDATE]
src/Vestigium.Suite.Windows.AppMgmtIQ/Views/DashboardView.xaml.cs      [UPDATE]
```

When this plan finishes, move the whole `PR02/` folder to `Complete/PR02/`.

---

## What each watch

| Role | Watch |
|---|---|
| Alvin | No write paths. No façade that bypasses Helper long-term. Catalog lands in Helpers, not the host. |
| Theodore | Missing keys or empty folders do not throw. Grid stays themed. QueryBar uses existing styles. Dashboard section uses existing ChartView helpers. |
| Simon | Both views and both Startup folders are included. Package link is best-effort only. Dashboard cards match the grid counts. Only bound queries enter MRU. |

---

## Next action

PR02.002. Enumerate Run / RunOnce / RunOnceEx.
