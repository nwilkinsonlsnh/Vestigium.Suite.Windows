# FXplorerIQ — PR04 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PLAN-PR04
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**APPID:** `FXplorerIQ`
**Status:** Draft
**Date:** 22 October 2026
**Binding:** PR04 Requirements wins on behavior. This file wins on the slice.

**Goal:** The verbs, through FileIo, with a job window, verify, and an operation log.

**Not:** A queue.

---

## Decision

| Call | Why |
|---|---|
| FileIo owns the work | The host is chrome. |
| Menu, not buttons | The Explorer context menu is the door. |
| Mirror (Verify) | Mirror always verifies. The name says so. |
| Delete and Delete (Secure) | A plain delete and a shred are different. |
| Verify is ALCOA+ | Both digests, written when the compare finishes. |
| Collision is a Settings tab | The job reads a default. UniqueName starts selected. |
| One correlation id | The FileIo job id. The main log points at the operation file. |
| Probe, then elevate | The host stays unelevated. Admin only when the probe fails. |
| Job is a modal | Phase, overall, current file, grid, and the five bins. |

---

## Menu

| Item | FileIo | Host rule |
|---|---|---|
| Copy | `FileIoHelper.Copy` | No purge. Collision from Settings. |
| Copy (Verify) | Copy, then `CompareFiles` | Source stays. Failed verify is a failed file. |
| Move | `FileIoHelper.Move` | Source removed after the write. |
| Move (Verify) | Move, then `CompareFiles` | Source removed only if the digests match. |
| Delete | `FileIoHelper.Delete` | Selection only. |
| Delete (Secure) | Delete with `Shred` | Recipe, then delete. |
| Mirror (Verify) | `FileIoHelper.Mirror` with Purge, then `CompareFiles` | Confirm first. Always verified. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR04.001 | Guards. Source exists. Destination can be created. Destination is not inside the source. Paths differ. Delete has no destination guard. | |
| 2 | PR04.002 | Collision tab in Settings. UniqueName, Skip, Overwrite. Pattern. Ask before Overwrite. Verify algorithm. | |
| 3 | PR04.003 | Context menu. Copy, Copy (Verify), Move, Move (Verify), Delete, Delete (Secure), Mirror (Verify). | |
| 4 | PR04.004 | Copy. FileIoHelper.Copy. Collision from Settings. No purge. | |
| 5 | PR04.005 | Copy (Verify). Copy, then compare. Record both digests. Source stays. | |
| 6 | PR04.006 | Move. FileIoHelper.Move. | |
| 7 | PR04.007 | Move (Verify). Move, then compare. Delete the source only if the digests match. | |
| 8 | PR04.008 | Delete. FileIoHelper.Delete. Selection only. | |
| 9 | PR04.009 | Delete (Secure). Shred recipe, then delete. | |
| 10 | PR04.010 | Mirror (Verify). Mirror with Purge. Confirm. Then verify. | |
| 11 | PR04.011 | Audit Mode. Same recon. No disk change. | |
| 12 | PR04.012 | Job modal. Phase, overall, current file, grid, five bins. Pause, resume, cancel. | |
| 13 | PR04.013 | Access probe. In process if it passes. Offer elevation if it fails. `FXplorerIQ.FileOps` for the elevated job. | |
| 14 | PR04.014 | Operation log. Correlation id, operation file, start and end lines in the main log with source and destination. | |
| 15 | PR04.015 | Job options. Masks, size, age, depth, empty directories, timestamps, retry. | |
