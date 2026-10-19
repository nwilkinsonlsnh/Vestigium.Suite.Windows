# PR04 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PR04-REQ  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**Status:** Draft  
**Date:** 19 October 2026  
**Product papers:** [Requirements_v1.0.md](../Requirements_v1.0.md), [Design_v1.0.md](../Design_v1.0.md)

This file is the **definition of done for PR04**. It adds lifecycle and health information to the existing inventory so an operator can see repair/modify options, detect orphaned uninstall entries, and compare versions against a simple baseline.

**One sentence:** Extend the package grid and details with Repair/Modify paths, an orphan indicator, and a version-baseline comparison so AppMgmtIQ moves from pure inventory toward application management.

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

---

## Must not change in PR04

- Automatic repair or cleanup of orphans
- Full software deployment or update orchestration
- Vulnerability database integration
- Changes to Startup (PR02) or Footprints (PR03) behavior beyond shared package linkage

---

## Acceptance

1. Packages with ModifyPath show a Modify action that launches after confirmation.
2. Repair action appears when a reliable repair command can be built.
3. Orphan indicator appears for packages whose install location is missing.
4. Version comparison column or indicator works against a simple baseline.
5. No automatic destructive actions.
6. Existing uninstall flow remains unchanged.

---

## Decision

| Call | Why |
|---|---|
| Explicit confirmation for Repair/Modify | Same safety model as uninstall. |
| Orphan is an indicator only | Cleanup is a separate decision. |
| Simple baseline | Enough for lab use without a full CMDB. |

### Rejected

| Idea | Why out |
|---|---|
| Auto-remove orphans | Too dangerous. |
| Full patch management | Out of scope for this host. |

---

## Parked

- Integration with external vulnerability feeds
- Bulk repair
- Scheduled baseline checks
