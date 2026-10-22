# PR06 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PR06-REQ
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`
**Status:** Draft
**Date:** 20 October 2026
**Product papers:** [Requirements_v1.0.md](../Requirements_v1.0.md), [Design_v1.0.md](../Design_v1.0.md)

This file is the **definition of done for PR06**. A baseline becomes a snapshot of every inventory tab, not a per-package version with no way to save it.

**One sentence:** Capture the current state of AppMgmtIQ, Windows Features, Built-In (Packages), Active Setup, Startup, and Footprints, then compare a later read against that snapshot.

**This version is not** a cleanup tool. Not an automatic repair. The snapshot is read and compared. The operator acts.

---

## Must change

### R06-01 One snapshot, six inventories

A baseline is one named snapshot. It holds a row for each inventory that has content.

| Inventory | Identity | Compared |
|---|---|---|
| AppMgmtIQ | Hive and key | Version |
| Windows Features | Feature name | Enabled state |
| Built-In (Packages) | Package family | Installed, provisioned |
| Active Setup | Component id | Version, installed |
| Startup | Scope and key | Enabled, command |
| Footprints | Kind, scope, and path | Exists |

Export, Dashboard, Settings, and Help are not snapshotted. They have no inventory.

### R06-02 Capture

The operator captures a baseline from the Dashboard. One action writes all six inventories from the data already loaded. The snapshot is named with the machine and the time. The operator can name it.

The existing per-package `baselines.json` is retired. The new store is one file per snapshot.

### R06-03 Compare

A later read is compared against the selected snapshot. Each row is one of:

- **Match.** Present in both, compared field is the same.
- **Changed.** Present in both, compared field differs.
- **Added.** In the current read, not in the snapshot.
- **Removed.** In the snapshot, not in the current read.

A row with no snapshot is **none**, as today.

### R06-04 Show the drift

The Dashboard Baseline chart splits by match, changed, added, removed, and none, for the selected inventory. The grid on each inventory tab gains a Baseline column. A capture with no later change reads as all match.

### R06-05 Select and clear

The operator can select which snapshot to compare against, and can delete a snapshot. There is no automatic capture.

---

## Must not change in PR06

- The inventories themselves
- Repair, uninstall, and the elevated appliers
- Export contents
- Automatic action on a drift

---

## Acceptance

1. One capture writes all six inventories.
2. A second read with no change reports match for every snapshotted row.
3. Installing, removing, enabling, or disabling a row reports added, removed, or changed on the right inventory.
4. The Dashboard Baseline chart shows the split, not a single None.
5. A snapshot can be selected and deleted.
6. No inventory is modified by the capture or the compare.

---

## Decision

| Call | Why |
|---|---|
| One snapshot, six inventories | A baseline of the machine, not of one tab. |
| Capture from the Dashboard | That is where the chart lives. |
| Compare fields stay small | Version, enabled, installed, command, exists. Not the whole row. |
| No automatic capture | The operator chooses the moment. |

### Rejected

| Idea | Why out |
|---|---|
| Keep per-package `baselines.json` | It cannot hold the other five inventories. |
| Snapshot Export, Dashboard, Settings | No inventory. |
| Act on drift | Out of scope. The operator acts. |
