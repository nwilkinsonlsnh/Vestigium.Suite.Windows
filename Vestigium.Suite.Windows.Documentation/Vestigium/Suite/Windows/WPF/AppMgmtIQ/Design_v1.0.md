# AppMgmtIQ — Design v1.0

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-SDD-001
**Version:** 1.0
**Status:** Release 1
**Date:** 21 October 2026
**Project:** Vestigium.Suite.Windows.AppMgmtIQ

This document describes how Release 1 is built. It matches the application as it is.

---

## 1. Boundary

The suite is a thin host. It owns the window, the views, the view models, the theme, and the binding. Reading and writing that can be shared lives in a helper. A write that needs elevation lives in a worker, which is a separate executable.

| Project | Owns |
|---|---|
| AppMgmtIQ | The window, the six inventories, export, snapshot, dashboard, settings, help. |
| AppMgmtIQ.Features | Elevated enable and disable of Windows features. |
| AppMgmtIQ.Bloatware | Elevated changes to built-in packages. |
| AppMgmtIQ.ActiveSetup | Elevated changes to machine Active Setup keys. |
| AppMgmtIQ.Startup | Elevated changes to machine startup entries. |

The host does not stay elevated. It writes a job, launches the worker, and reads the result.

---

## 2. Startup

1. Logging, with APPID `AppMgmtIQ`.
2. ThemeManager, before the window parses.
3. The shell, the view models, and the views.
4. Each inventory loads on its own. A loading overlay covers the tab until the read ends.

---

## 3. Shell

`AppMgmtIqWindow` hosts a Vestigium shell. Each navigation item is a `VestigiumNavItemSpec`. The content of an item is the view for that tab. The status bar is the shell status bar. A tab reports its own status into it.

---

## 4. Inventories

Each inventory is a view model and a view. The view model owns the rows, the query session, and the load. The view owns the grid or the tree, the query bar, and the context menu.

### 4.1 AppMgmtIQ

`MainViewModel` reads the four uninstall trees through the registry helper. A row is a `PackageRow`. Computed fields are architecture, scope, orphan, and pending reboot.

Orphan resolution uses the path helper. A candidate is cleaned, tokens are left alone, and a file is tested with `File.Exists`. An executable with more than one dot is still a file.

Details are a second window. It binds to the selected row and shows every registry value, grouped.

### 4.2 Windows Features

`FeaturesViewModel` reads the component store and builds a tree of `FeatureNode`. A checkbox writes an enable or a disable into a plan. Apply writes a `FeatureJob` and launches `AppMgmtIQ.Features`.

The tree columns share a size. Display name takes the remaining width. Feature name, parent, and state are fixed.

### 4.3 Built-In (Packages)

`BloatwareViewModel` holds the catalog and the machine state. A row is a family. Installed and provisioned come from the machine. A change writes a `BloatwareJob` and launches `AppMgmtIQ.Bloatware`.

### 4.4 Active Setup

`ActiveSetupViewModel` reads the Installed Components keys. Pending is computed by comparing the machine version with the user version. A user-key change is written directly. A machine-key change writes an `ActiveSetupJob` and launches `AppMgmtIQ.ActiveSetup`.

### 4.5 Startup

`StartupViewModel` reads Run, RunOnce, RunOnceEx, the Startup folders, and the disabled locations. Disable moves an entry to AutorunsDisabled. Enable moves it back.

A user write is done in process. A machine write launches `AppMgmtIQ.Startup`. A mixed action splits the work.

### 4.6 Footprints

`FootprintsViewModel` reads App Paths, SharedDLLs, Products, and Associations. The grid is filled from the registry read. A background pass then sets Exists for paths that look like files.

Package linking is best effort. A path inside an install location matches. A product code that equals an uninstall key matches. A display name of at least four characters matches when no path is available.

---

## 5. Query

Each view model holds a `KqlSession` from the matching catalog.

| Tab | Catalog |
|---|---|
| AppMgmtIQ | `Vestigium.Helpers.Kql.AppMgmt` packages |
| Windows Features | features |
| Built-In | bloatware |
| Active Setup | `Vestigium.Helpers.Kql.ActiveSetup` |
| Startup | startup |
| Footprints | footprints |

`KqlHost` compiles the text. The query bar commits into the view model, which filters the rows. The Baseline column is not shown. `app.BaselineStatus` remains a field on the package session.

---

## 6. Snapshot

### 6.1 Store

`SnapshotStore` writes one JSON file per capture under `C:\ProgramData\Vestigium\Snapshots\AppMgmtIQ`. The name is the machine and the time. A file holds six lists of `SnapshotRow`, one per inventory. A row is a key and a value.

On first open, files still sitting in the old desktop folder are copied across if the target does not already have them.

### 6.2 Choice

`SnapshotChoice` remembers which inventories a capture includes. It is a JSON file in the snapshot folder. All six default to on.

### 6.3 Compare

`SnapshotCompare` takes the saved list and the current list and returns a drift for each key. Match, changed, added, removed. None is applied when no snapshot is selected.

`BaselineApply` runs the compare and sets the status on each row. It also remembers the selected snapshot name.

### 6.4 Selection

The Snapshot tab lists the captures. Choosing one sets `BaselineApply.Selected`, which raises `Changed`. The dashboard listens and rebuilds. Capture, import, and delete do the same.

Import reads a JSON file, saves it through the store, and selects it. Export copies the selected file to a path the operator chooses.

---

## 7. Dashboard

`DashboardView` binds the six view models. A section reads the rows already loaded and builds cards and charts.

Charts are built through `ChartTheme`, so the background, the axes, the grid, and the series come from the active Vestigium brushes. `ThemeManager.ThemeChanged` rebuilds them. `BaselineApply.Changed` rebuilds them.

The Snapshots section loads the selected snapshot, counts the inventories it holds, and charts coverage and drift.

The cumulative line on a ranked chart uses the theme text color, so it stays visible on a dark theme and on a light one.

---

## 8. Export

`ExportViewModel` holds the include choices and the current-view choices. `InventoryExport` writes the workbook through the spreadsheet helper. The summary sheet is first. Package details is optional.

Choices are part of `AppMgmtSettings` and survive a restart.

---

## 9. Elevation

A worker is a separate WPF executable with its own manifest. The host writes a job JSON to `C:\ProgramData\Vestigium\Jobs\AppMgmtIQ` and starts the worker with `runas`. The worker reads the job, does the work, writes a result beside the job, and exits. The host reads the result and reports it.

Cancelling the elevation prompt is not a failure of the user-scoped writes. Those are already done. The machine writes are reported as cancelled.

---

## 10. Settings

`AppMgmtSettingsStore` reads and writes `AppMgmtSettings` as JSON under `C:\ProgramData\Vestigium\Settings\AppMgmtIQ`.

`SettingsViewModel` binds the theme, the display flags, the date format, the decimal places, and the query history limits. Changing the theme calls `ThemeManager.SwitchTheme`.

---

## 11. Help

`HelpCopy` holds the topics. A topic may have sections, which the help view shows as sub-tabs. The KQL topic has one section per inventory. The license topic reads the license file.

---

## 12. Theming

The host uses Vestigium.Themes. Buttons, menus, separators, the query bar, the dropdown, and the grids take their styles from the theme. Charts take their colors from the theme tokens through `ChartTheme`. A missing token falls back to the chart library's documented color.

---

## 13. Logging

`HostLog.Initialize` runs first. Inventory loads, apply requests, and worker results are logged through the host catalog. A worker logs under its own identity.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 16 Oct 2026 | First lock. Host and the package inventory. |
| 1.0 | 21 Oct 2026 | Release 1. Workers, six inventories, query, snapshot, dashboard, export. |
