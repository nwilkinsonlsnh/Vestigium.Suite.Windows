# PR02 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PR02-REQ  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**Status:** Draft  
**Date:** 19 October 2026  
**Product papers:** [Requirements_v1.0.md](../Requirements_v1.0.md), [Design_v1.0.md](../Design_v1.0.md)

This file is the **definition of done for PR02**. It adds a Startup / Persistence view so AppMgmtIQ can show what installed software (or other processes) registered to run automatically, filter it, and turn those entries on or off.

**One sentence:** Enumerate Run, RunOnce, RunOnceEx, and Startup folder entries, show them in a grid with source, command, and enabled state, link each entry to an installed package when a match exists, filter with the QueryBar, surface a Dashboard section, and let an administrator enable or disable entries (one or all in the current view) by writing the Windows StartupApproved flag. HKCU writes stay in the host. HKLM and common Startup folder writes go through a separate elevated project.

**This version is not** a Scheduled Task editor. Not a service controller. Not a private cache of deleted values. Disable is the Windows flag, not a tool-owned subkey. The main application is not elevated.

---

## Inventory sources

The host (via Helper or temporary host walk) enumerates:

### Registry (both 32-bit and 64-bit views where applicable)

- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce`
- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnceEx`
- `HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run`
- `HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\RunOnce`

### Folders

- `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup` (per-user)
- `%ProgramData%\Microsoft\Windows\Start Menu\Programs\Startup` (common)

### Enable flag

Windows stores the on/off state next to the entry, not by deleting it:

- `...\Explorer\StartupApproved\Run`
- `...\Explorer\StartupApproved\Run32`
- `...\Explorer\StartupApproved\StartupFolder`

First byte `02` or `06` means Enabled. Anything else means Disabled. No flag means Enabled. RunOnce and RunOnceEx have no disable flag; they run once.

Entries with no display name use the value name or file name as the fallback.

---

## Must change

### R02-01 Startup grid

New tab or page: **Startup**.

Columns (minimum):

| Column | Source |
|---|---|
| Name | Value name or file name |
| Command | Registry value data or shortcut target |
| Source | Run / RunOnce / RunOnceEx / Startup folder |
| Scope | HKLM / HKCU / Common / User |
| Bit | 64 / 32 (for registry) |
| State | Enabled / Disabled |
| Package | Linked installed package DisplayName when matched, else blank |

Single selection.

### R02-02 Package linkage

When the command path or value name can be matched to an installed package (by UninstallString path, InstallLocation, or DisplayName token), show the package name. Otherwise leave blank. Matching is best-effort, not perfect.

### R02-03 Details

Selection shows the raw command, the source key or folder path, and the enabled state.

### R02-04 Chrome

Startup tab appears in the main navigation alongside existing tabs. Theme and status bar unchanged. The main application stays `asInvoker`.

### R02-05 QueryBar and KQL catalog

The Startup view hosts a `VestigiumQueryBar` wired the same way as the existing inventory, features, bloatware, and Active Setup views (`FilterRequested`, `Committed`, `PinRequested`, `Session`, `Queries`, `Limit`).

A Startup field catalog lands in `Vestigium.Helpers.Kql.AppMgmt` and is exposed as `AppMgmtKql.Startup()`. Minimum fields:

| Canonical | Type | Notes |
|---|---|---|
| `startup.name` | String | Value or file name |
| `startup.command` | String | Registry data or shortcut target |
| `startup.source` | String | Run / RunOnce / RunOnceEx / Startup folder |
| `startup.scope` | String | HKLM / HKCU / Common / User |
| `startup.isX64` | Boolean | Registry view |
| `startup.package` | String | Linked package name, blank if none |
| `startup.enabled` | Boolean | StartupApproved flag. Missing flag is true. |

Live filter on typing (debounced). Commit (Enter or saved-apply) records the query in MRU only when the query binds successfully. Same `QueryMru` limits and sticky behavior as the other views.

### R02-06 Dashboard Startup section

The Dashboard gains a **Startup** radio section alongside Packages / Features / Bloatware / Active Setup. It binds to the same Startup collection.

Cards (minimum): total entries, enabled, disabled, linked vs unlinked.

Charts (minimum): state pie, source breakdown, scope pie, linked vs unlinked.

Story text updated. Cards match the grid counts.

### R02-07 Enable and disable

Disable and Enable write the Windows StartupApproved flag. The Run value stays. The entry stays in the grid and stays linked to its package.

Context menu:

| Action | Does |
|---|---|
| Enable | Sets the flag to enabled for the selected row. |
| Disable | Sets the flag to disabled for the selected row. |
| Enable All | Sets the flag to enabled for every row in the current filtered view. |
| Disable All | Sets the flag to disabled for every row in the current filtered view. |
| Remove | Deletes the registry value or Startup folder item. Confirm. Not the default. |
| Open source | Opens the registry key or the folder. |
| Jump to package | Selects the linked package in the inventory tab. Disabled when unlinked. |

Rules:

- Enable All and Disable All operate on the **current filtered rows**, not the full set. A filter of `startup.source == 'Run'` must not touch Startup folder entries.
- Confirm before a bulk action. The dialog shows the count and a scope split (HKCU vs HKLM).
- RunOnce and RunOnceEx are skipped by Enable and Disable. They have no flag. Remove is the only write for them.
- Every write logs name, source, scope, before, after, and result.
- No silent bulk write. No "disable everything on the machine" without the confirm.

AutorunsDisabled is not used. It is a Sysinternals convention. Windows does not read it, and other tools treat the entry as gone rather than disabled.

### R02-08 Elevation boundary

The main application is not elevated. Elevation is a separate project, the same shape as `AppMgmtIQ.Features` and `AppMgmtIQ.Bloatware`.

| Write | Where |
|---|---|
| HKCU Run, HKCU Startup folder | Host, in process. No UAC. |
| HKLM Run, HKLM RunOnce, common Startup folder | `Vestigium.Suite.Windows.AppMgmtIQ.Startup`. `requireAdministrator`. |

The elevated project is `Vestigium.Suite.Windows.AppMgmtIQ.Startup`. Assembly `AppMgmtIQ.Startup`. WinExe. Manifest `requireAdministrator`. It is launched only when an HKLM or common write is in the job.

Contract matches Features:

1. Host writes a job file (correlation id, items, intended state).
2. Host starts `AppMgmtIQ.Startup.exe` with `Verb = runas` and the job path.
3. The elevated exe applies the StartupApproved writes (or the remove) and writes a result file.
4. Host reads the result, refreshes the rows, and reports succeeded / denied / cancelled.

A bulk action that mixes HKCU and HKLM does the HKCU writes in process and the HKLM writes in one elevated launch. One UAC prompt per elevated batch, not one per row.

If the operator cancels UAC, the HKLM items are reported as cancelled. The HKCU items already written stay written.

---

## Must not change in PR02

- Scheduled Tasks (parked)
- Services (parked)
- Full App Paths or file-association walk (later PR)
- Private backup store of deleted values
- Writes that are not confirmed
- Elevating the main application

---

## Acceptance

1. Startup tab lists entries from all listed registry keys and both Startup folders.
2. Source, Scope, Bit, and State columns are correct for the location.
3. Package column shows a match when one is reasonably available; blank otherwise.
4. QueryBar filters the grid against the Startup catalog, including `startup.enabled`. Only successfully bound queries are recorded in MRU.
5. Dashboard Startup section shows cards and charts that match the grid counts, including enabled and disabled.
6. Enable and Disable flip the StartupApproved flag. The Run value remains.
7. Enable All and Disable All act only on the current filtered rows, after a confirm that shows the count.
8. An HKCU write does not prompt for elevation.
9. An HKLM or common write launches `AppMgmtIQ.Startup`. Cancelling UAC is reported, not thrown.
10. A mixed bulk action uses one elevated launch for the HKLM items.
11. Existing inventory, features, bloatware, and Active Setup tabs remain unchanged.
12. Host tests stay off the live registry for these new paths.

---

## Decision

| Call | Why |
|---|---|
| StartupApproved for disable | Windows flag. Task Manager, Settings, and other tools agree. |
| Bulk on the filtered view | Streamlines administration without surprising the operator. |
| Confirm on bulk | Count and scope are visible before the write. |
| Separate elevated project | Same pattern as Features and Bloatware. Main app stays unelevated. |
| One UAC per elevated batch | Not one prompt per row. |
| Best-effort package link | Useful context without requiring perfect matching. |
| Both views for Run | 32-bit and 64-bit Run keys both exist. |
| Startup folders included | Common place installers drop shortcuts. |
| QueryBar + catalog in this PR | Same filter experience as existing tabs. |
| Dashboard section in this PR | Summary stays in sync with the new tab. |

### Rejected

| Idea | Why out |
|---|---|
| AutorunsDisabled subkey | Tool convention. Windows does not read it. Entry vanishes from Task Manager. |
| Delete as the disable | Not reversible by other tools. |
| Scheduled Tasks in the same grid | Different store and permissions. Parked. |
| Perfect package matching | Too brittle. Best-effort is enough. |
| Silent Disable All | Operator must see the count first. |
| Elevate the main application | Elevates reads and UI that do not need it. |
| UAC per row | Noisy. One launch per batch. |

---

## Parked (not required to close PR02)

- Scheduled Tasks
- Services tied to installed products
- RunOnceEx processing order
- Add a new startup entry from the UI
