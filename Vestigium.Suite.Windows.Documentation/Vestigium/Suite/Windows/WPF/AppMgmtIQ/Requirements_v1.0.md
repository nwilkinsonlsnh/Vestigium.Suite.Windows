# AppMgmtIQ — Requirements v1.0

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-SRS-001
**Version:** 1.0
**Status:** Release 1
**Date:** 21 October 2026
**Project:** Vestigium.Suite.Windows.AppMgmtIQ
**Kind:** WPF, net10.0-windows, MVVM

This document states what Release 1 of AppMgmtIQ does. It describes the application as it is, not as it was planned.

---

## 1. Purpose

AppMgmtIQ is a single window for the inventories Windows keeps apart. It reads installed packages, optional Windows features, built-in and Store packages, Active Setup, startup entries, and the side effects an installer leaves behind. It filters each inventory with the same query language, writes a workbook, and can save a snapshot of what it read so a later read can be compared with it.

Reading does not require elevation. A change that must write to a machine location is handed to a short-lived elevated worker. The suite shows the result and does not stay elevated.

---

## 2. Release 1 scope

Release 1 includes:

- Six inventories, each on its own tab.
- A query bar on each inventory that speaks KQL.
- An export of the loaded inventories to an Excel workbook.
- A snapshot of the loaded inventories, with compare, import, and export.
- A dashboard that reads the loaded inventories and the selected snapshot.
- Settings for theme, display, and the limits of the query history.
- Help for every inventory, for KQL, and for the license.

Release 1 does not include a package editor, a registry editor, or a cleanup tool. Footprints is a reading. Orphan is a flag, not a repair.

---

## 3. Shell

The window is a Vestigium shell. The top navigation, in order, is:

1. AppMgmtIQ
2. Windows Features
3. Built-In (Packages)
4. Active Setup
5. Startup
6. Footprints
7. Export
8. Snapshot
9. Dashboard
10. Settings
11. Help

The status bar reports the work of the selected tab. A loading overlay covers a tab while its inventory is being read.

---

## 4. AppMgmtIQ

This tab is the installed-package inventory. It reads the four uninstall trees:

- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall`
- `HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall`
- `HKCU\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall`

One row is one key that has something to show. A key with no display name uses the key name.

The grid shows display name, publisher, version, install date, estimated size, architecture, scope, and the operational flags. Double-click a row and a details window shows every registry value on that key, grouped, plus the computed fields. The window title is the display name and the version.

Computed fields:

- Architecture is 64-bit unless the key is under WOW6432Node.
- Scope is machine for HKLM and user for HKCU.
- Orphan is true when every candidate path for the package is missing. A path is resolved before it is tested, including an executable named with more than one dot.
- Pending reboot is true when a pending file rename names the package.

The context menu can uninstall, force an MSI uninstall, and clear a NoRemove flag. An uninstall uses the stored uninstall string. A forced uninstall is available only for a Windows Installer package and builds an `msiexec /x` command.

---

## 5. Windows Features

This tab reads the optional component store. It shows a tree. A parent owns its children. Each row shows the display name, the feature name, the parent, and the state.

A checkbox plans an enable or a disable. The plan is applied by an elevated worker. The host writes a job and launches the worker. The worker enables and disables, writes a result, and exits.

The tree can be expanded or collapsed, one node or all. A filter can show enabled, disabled, and absent.

---

## 6. Built-In (Packages)

This tab reads a catalog of inbox and Microsoft Store families, then asks the machine which of them are installed and which are provisioned. A row that is not installed is a catalog entry the machine does not have. It is not an error.

The grid shows name, family, category, installed, provisioned, and package name. Categories are System, Store, Games, Media, and Productivity.

A change is handed to an elevated worker.

---

## 7. Active Setup

This tab reads Active Setup components under `SOFTWARE\Microsoft\Active Setup\Installed Components`, in HKLM and HKCU, and again under WOW6432Node.

Pending means the machine copy is ahead of this user's copy, so the command in StubPath will run at the next logon. A component with no StubPath has nothing to run.

The menu can add, delete, and clear the command. A user-key change is written in this process. A machine-key change is handed to an elevated worker. Delete removes the key. Clear command removes StubPath and leaves the version.

---

## 8. Startup

This tab reads what starts with Windows:

- Run, RunOnce, and RunOnceEx, in HKLM and HKCU, and the 32-bit views.
- The per-user Startup folder and the common Startup folder.
- The approved and disabled locations Windows and Sysinternals use, including AutorunsDisabled.

Each row shows name, command, source, scope, architecture, whether it is enabled, and the package it linked to when a match was confident.

Disable moves an entry to the AutorunsDisabled location. Enable moves it back. A user write is done in this process. A machine write is handed to an elevated worker. A mixed action does the user writes here and the machine writes in one elevated launch. Cancelling the prompt leaves the user writes as they are and reports the machine writes as cancelled.

Disable All and Enable All act on the current view.

---

## 9. Footprints

This tab is a read-only audit of installer side effects. It is not a registry editor.

It reads four kinds:

- App Paths, under CurrentVersion\App Paths, in HKLM, the 32-bit view, and HKCU.
- SharedDLLs, in HKLM and the 32-bit view. The value is the reference count.
- Products, under Installer\UserData. A packed name is unpacked to a GUID. The SYSTEM SID is a machine install. Any other SID is a per-user install.
- Associations, installer-linked ProgIDs and their extensions, in HKLM and HKCU. A ProgID is included only when it carries an install source or a product code.

The grid fills as soon as the registry read ends. A background pass then checks whether a path that looks like a file still exists. Product codes and ProgIDs are skipped. Exists is Yes, No, or blank. Blank means the check has not finished, or the value is not a file.

The Package column links a footprint to a known package when a path sits inside an install location, a product code matches an uninstall key, or a display name of at least four characters matches and no path is available.

---

## 10. Query

Each inventory has a query bar. The language is KQL.

A bare word searches the text columns. A named field searches that field. Comparison is `==`, `!=`, `>`, `<`, `>=`, `<=`. Text can use `has`, `startswith`, `endswith`, `LIKE`, `CONTAINS`, and `BEGINS WITH`. Clauses combine with `and`, `or`, and `not`.

The fields for each tab are listed in Help, under KQL. The Baseline field on AppMgmtIQ is `app.BaselineStatus`. The column is hidden. The field is still queryable.

The query history is remembered. A query can be pinned. The history length and the pin length are settings.

---

## 11. Export

Export writes one Excel workbook from the inventories already loaded. It does not run a new scan.

The first sheet is a summary, and it is the sheet Excel opens on. A further sheet is added for each inventory that is included. Each inventory has its own include toggle, and its own toggle for current view or everything loaded. Current view respects the query and the filters. Everything loaded ignores them.

Package details adds a sheet of the registry values the details window shows, and it is available only when AppMgmtIQ is included.

The file is written under the desktop exports folder for this host. The choices on the page are remembered.

---

## 12. Snapshot

A snapshot is a saved reading of the inventories. Capture writes what is loaded, for the inventories that are toggled on. A later read is compared with it.

One file is one capture, named with the machine and the time. Files live in `C:\ProgramData\Vestigium\Snapshots\AppMgmtIQ`.

Compare produces one of five results for each row:

- match, present on both sides with the same value
- changed, present on both sides with a different value
- added, present now and not in the snapshot
- removed, present in the snapshot and not now
- none, no snapshot is selected

What is compared:

- AppMgmtIQ, by version
- Windows Features, by enabled
- Built-In, by installed and provisioned
- Active Setup, by version and installed
- Startup, by enabled and command
- Footprints, by whether the file exists

The dropdown selects the snapshot the dashboard compares against. The choice is remembered. Delete removes the selected snapshot. Export writes it to a file the operator chooses. Import reads a file from another machine, saves it, and selects it.

A capture that was written to the old desktop folder is copied into ProgramData the first time the store opens.

---

## 13. Dashboard

The dashboard is a reading of the inventories already loaded. It does not scan, and it has no refresh of its own. Opening it, or switching its section, redraws the cards and the charts.

Each inventory has a section. A section with two charts shows them together. A section with more than two offers a list, and the area shows the one that is picked.

The Snapshots section reads the selected snapshot. It shows how many snapshots are on disk, which one is selected, how many inventories it holds, and how many rows it contains. Coverage is rows per inventory. Drift is match, changed, added, and removed across all six.

Charts take their colors from the active theme. Changing the theme rebuilds them. Changing the selected snapshot rebuilds them.

---

## 14. Settings

Settings holds the theme, the display options, and the query history limits.

Display includes which package scopes and architectures are shown, whether system components are shown, the date format, and the number of decimal places. The decimal places value is centered in its field.

The choices are written under `C:\ProgramData\Vestigium\Settings\AppMgmtIQ` and survive a restart.

---

## 15. Help

Help has a topic for the overview, for each inventory, for the dashboard, for snapshots, for export, for KQL, for settings, for the glossary, for the license, and for about.

The KQL topic has a sub-tab for each inventory that has a query bar. Each sub-tab lists the fields, the aliases, and an example.

---

## 16. Elevation

A write that must touch a machine location is not done in the suite. The suite writes a job file and launches a worker with elevation. The worker reads the job, does the work, writes a result, and exits.

Workers:

- AppMgmtIQ.Features, for enable and disable.
- AppMgmtIQ.Bloatware, for built-in package changes.
- AppMgmtIQ.ActiveSetup, for machine-key changes.
- AppMgmtIQ.Startup, for machine startup changes.

Jobs are written to `C:\ProgramData\Vestigium\Jobs\AppMgmtIQ`.

A user-scoped write, such as an HKCU startup entry, is done in the suite. No elevation.

---

## 17. Paths

| What | Where |
|---|---|
| Snapshots | `C:\ProgramData\Vestigium\Snapshots\AppMgmtIQ` |
| Jobs | `C:\ProgramData\Vestigium\Jobs\AppMgmtIQ` |
| Settings | `C:\ProgramData\Vestigium\Settings\AppMgmtIQ` |
| Exports | Desktop, under Vestigium\Exports\AppMgmtIQ |

---

## 18. Logging

The host logs with APPID `AppMgmtIQ` through Vestigium.Logging. A worker logs under its own identity. A requested change, a completed change, and a cancelled elevation are logged.

---

## 19. Out of Release 1

- Editing a footprint, or deleting an orphaned registry value.
- A repair that puts a missing file back.
- winget, or any package source other than the inventories above.
- A scheduled capture. Capture is an action the operator takes.
- Staying elevated. Elevation is per job.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 16 Oct 2026 | First lock. Package grid and details. |
| 1.0 | 21 Oct 2026 | Release 1. Six inventories, query, export, snapshot, dashboard, settings, help, and elevation. |
