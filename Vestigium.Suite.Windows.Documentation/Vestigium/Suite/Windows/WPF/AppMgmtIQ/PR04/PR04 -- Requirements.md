# PR04 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PR04-REQ  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**Status:** Draft  
**Date:** 19 October 2026  
**Product papers:** [Requirements_v1.0.md](../Requirements_v1.0.md), [Design_v1.0.md](../Design_v1.0.md)

This file is the **definition of done for PR04**. It adds lifecycle and health information to the existing inventory so an operator can see repair/modify options, detect orphaned uninstall entries, and compare versions against a simple baseline. The existing QueryBar continues to filter the grid, now against the extended Package catalog. The Dashboard Packages section (or a Lifecycle section) summarizes the new signals.

**One sentence:** Extend the package grid and details with Repair/Modify paths, an orphan indicator, and a version-baseline comparison so AppMgmtIQ moves from pure inventory toward application management, with the new fields queryable through the existing QueryBar and visible on the Dashboard.

**This version is not** an automatic repair engine. Not a software deployment system. Not a vulnerability scanner. It surfaces the data and offers explicit, confirmed actions where already supported (uninstall) or newly added (repair/modify launch).

---

## Must change

### R04-01 Repair and Modify paths

For packages that have them, surface:

- `ModifyPath`
- `QuietUninstallString` (already partially present)
- Windows Installer repair command when a product code is known

Details window or context menu offers **Repair** and **Modify** when the path exists. Launch uses the same confirmation pattern as uninstall.

### R04-02 Orphan indicator

A package row is marked orphaned when the Uninstall key exists but the expected install location or key executable is missing. Indicator appears in the grid and details. No automatic cleanup.

### R04-03 Version baseline (optional simple)

Allow an operator to mark a package version as a baseline (or load a simple expected-version list). Grid shows a comparison indicator (match / newer / older / unknown). No automatic update.

### R04-04 Pending reboot / file rename

Surface pending file rename operations or reboot-required flags when the Helper or host can read them without side effects.

### R04-05 Extend Package KQL catalog

The existing `VestigiumQueryBar` on the packages view stays. The Package catalog in `Vestigium.Helpers.Kql.AppMgmt` gains fields so the new lifecycle signals are queryable. Minimum additions:

| Canonical | Type | Notes |
|---|---|---|
| `app.IsOrphan` | Boolean | Missing install location or key file |
| `app.HasModify` | Boolean | ModifyPath present |
| `app.HasRepair` | Boolean | Repair command can be built |
| `app.BaselineStatus` | String | match / newer / older / none |
| `app.PendingReboot` | Boolean | When readable |

Existing fields remain. Live filter and commit behavior unchanged. Only successfully bound queries enter MRU.

### R04-06 Dashboard lifecycle signals

Extend the existing Packages Dashboard section (or add a Lifecycle radio if the section becomes crowded). Bind to the same package collection.

Cards (minimum): orphans, packages with Repair available, packages with Modify available, baseline match / newer / older / none.

Charts (minimum): orphan vs clean, baseline status breakdown, action availability.

Story text updated to mention health. Cards match the grid counts.

### R04-07 Resolve footprint paths before Exists and orphan checks

`PathResolve` already strips quotes, drops a comma index and a `\??\` prefix, and expands environment variables. That is not enough. An export of this machine showed three gaps.

- **App Path blanks.** About 20 App Path keys have an empty default value. The key name is the executable (`cmmgr32.exe`, `dfshim.dll`, `Snagit`). Read the `Path` value on the same key and join it to the key name. If that is still empty, search `System32`, `SysWOW64`, and PATH for the key name. An empty default means search normally, so a hit is a resolution.
- **Product blanks.** About 405 Product rows carry a product code, not a file. Look up `Installer\UserData\...\InstallProperties` for `InstallLocation` and `LocalPackage`. If either is a real path, answer Exists from it.
- **SharedDll No.** About 8 SharedDll rows resolved and `File.Exists` said no. Retry once under Wow64 redirection (`System32` to `SysWOW64`). Optionally check WinSxS for side-by-side names (`d3d11*sdklayers`, `d3d11ref`). A row that still misses is a genuine leftover.

Blank stays blank only when no path can be built. No means a path was built and the file is not there. Do not write No for a product code or an empty App Path that was never searched.

---

## Must not change in PR04

- Automatic repair or cleanup of orphans
- Full software deployment or update orchestration
- Vulnerability database integration
- Changes to Startup (PR02) behavior beyond shared package linkage
- Footprints (PR03) behavior other than path resolution for Exists and the shared orphan check

---

## Acceptance

1. Packages with ModifyPath show a Modify action that launches after confirmation.
2. Repair action appears when a reliable repair command can be built.
3. Orphan indicator appears for packages whose install location is missing.
4. Version comparison column or indicator works against a simple baseline.
5. QueryBar can filter on the new catalog fields (`app.IsOrphan`, `app.HasModify`, `app.HasRepair`, `app.BaselineStatus`, `app.PendingReboot`).
6. Dashboard shows orphan, action, and baseline cards/charts that match the grid.
7. No automatic destructive actions.
8. Existing uninstall flow remains unchanged.
9. App Path rows with an empty default are resolved from the Path value or a System32 / SysWOW64 / PATH search before Exists is left blank.
10. Product rows answer Exists from InstallLocation or LocalPackage when one is present.
11. SharedDll No rows are retried under Wow64 redirection before they stay No.

---

## Decision

| Call | Why |
|---|---|
| Explicit confirmation for Repair/Modify | Same safety model as uninstall. |
| Orphan is an indicator only | Cleanup is a separate decision. |
| Simple baseline | Enough for lab use without a full CMDB. |
| Extend existing Package catalog | QueryBar is already on the packages view. New fields only. |
| Extend Packages Dashboard section | Avoid a new radio for data that is a property of packages. Optional Lifecycle radio only if crowded. |

### Rejected

| Idea | Why out |
|---|---|
| Auto-remove orphans | Too dangerous. |
| Full patch management | Out of scope for this host. |
| New QueryBar for lifecycle | Packages view already has one. |
| Dedicated Dashboard PR later | Dashboard would lag the data. |

---

## Parked

- Integration with external vulnerability feeds
- Bulk repair
- Scheduled baseline checks
