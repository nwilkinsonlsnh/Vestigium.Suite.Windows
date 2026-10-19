# PR02 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PR02-REQ  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**Status:** Draft  
**Date:** 19 October 2026  
**Product papers:** [Requirements_v1.0.md](../Requirements_v1.0.md), [Design_v1.0.md](../Design_v1.0.md)

This file is the **definition of done for PR02**. It adds a Startup / Persistence view so AppMgmtIQ can show what installed software (or other processes) registered to run automatically, with KQL filtering via the shared QueryBar and a Dashboard summary.

**One sentence:** Enumerate Run, RunOnce, RunOnceEx, and Startup folder entries from the standard locations, show them in a grid with source and command, link each entry back to an installed package when a match exists, filter the grid with the QueryBar against a Startup KQL catalog, and surface a Dashboard section for the same data.

**This version is not** a startup manager that disables or deletes entries. Not a Scheduled Task editor. Not a service controller. Read-only inventory of persistence locations that commonly belong to installed applications.

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
| Package | Linked installed package DisplayName when matched, else blank |

Single selection. Read-only. No enable/disable controls in this PR.

### R02-02 Package linkage

When the command path or value name can be matched to an installed package (by UninstallString path, InstallLocation, or DisplayName token), show the package name. Otherwise leave blank. Matching is best-effort, not perfect.

### R02-03 Details

Double-click or selection shows the raw command and the source key/folder path.

### R02-04 Chrome

Startup tab appears in the main navigation alongside existing tabs. Theme and status bar unchanged.

### R02-05 QueryBar and KQL catalog

The Startup view hosts a `VestigiumQueryBar` wired the same way as the existing inventory, features, bloatware, and Active Setup views (`FilterRequested`, `Committed`, `PinRequested`, `Session`, `Queries`, `Limit`).

A Startup field catalog lands in `Vestigium.Helpers.Kql.AppMgmt` (or a sibling catalog project if the domain grows) and is exposed as a session (e.g. `AppMgmtKql.Startup()`). Minimum fields:

| Canonical | Type | Notes |
|---|---|---|
| `startup.name` | String | Value or file name |
| `startup.command` | String | Registry data or shortcut target |
| `startup.source` | String | Run / RunOnce / RunOnceEx / Startup folder |
| `startup.scope` | String | HKLM / HKCU / Common / User |
| `startup.isX64` | Boolean | Registry view |
| `startup.package` | String | Linked package name, blank if none |

Live filter on typing (debounced). Commit (Enter or saved-apply) records the query in MRU only when the query binds successfully. Same `QueryMru` limits and sticky behavior as the other views.

### R02-06 Dashboard Startup section

The Dashboard gains a **Startup** radio section alongside Packages / Features / Bloatware / Active Setup. It binds to the same Startup collection.

Cards (minimum): total entries, by source (Run / RunOnce / Startup folder), scope (HKLM vs HKCU), linked vs unlinked.

Charts (minimum): source breakdown, scope pie, linked vs unlinked.

Story text updated. Cards match the grid counts.

---

## Must not change in PR02

- Disable, delete, or edit any Run or Startup entry
- Scheduled Tasks (parked)
- Services (parked — separate helper exists)
- Full App Paths or file-association walk (PR03)
- Any write to the registry or file system for these locations

---

## Acceptance

1. Startup tab lists entries from all listed registry keys and both Startup folders.
2. Source, Scope, and Bit columns are correct for the location.
3. Package column shows a match when one is reasonably available; blank otherwise.
4. QueryBar filters the grid against the Startup catalog. Only successfully bound queries are recorded in MRU.
5. Dashboard Startup section shows cards and charts that match the grid counts.
6. No controls that modify Run keys or Startup folders.
7. Existing inventory, features, bloatware, and Active Setup tabs remain unchanged.
8. Host tests stay off the live registry for these new paths.

---

## Decision

| Call | Why |
|---|---|
| Read-only | Application management inventory, not a startup cleaner. |
| Best-effort package link | Useful context without requiring perfect matching. |
| Both views for Run | 32-bit and 64-bit Run keys both exist. |
| Startup folders included | Common place installers drop shortcuts. |
| QueryBar + catalog in this PR | Same filter experience as existing tabs. Catalog lives in Helpers so other hosts can reuse it. |
| Dashboard section in this PR | Summary stays in sync with the new tab. |

### Rejected

| Idea | Why out |
|---|---|
| Enable/disable toggles | Out of scope for inventory PR. |
| Scheduled Tasks in the same grid | Different store and permissions. Parked. |
| Perfect package matching | Too brittle. Best-effort is enough. |
| Dedicated Dashboard PR later | Dashboard would lag the data. |
| Host-local field list instead of catalog | Breaks the shared KQL pattern. |

---

## Parked (not required to close PR02)

- Scheduled Tasks
- Services tied to installed products
- RunOnceEx processing order
- Disable / remove actions
