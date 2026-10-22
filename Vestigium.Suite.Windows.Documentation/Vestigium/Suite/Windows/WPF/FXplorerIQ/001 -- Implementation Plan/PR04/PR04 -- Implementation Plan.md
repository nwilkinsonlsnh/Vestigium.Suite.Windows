# FXplorerIQ — PR04 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PLAN-PR04
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**APPID:** `FXplorerIQ`
**Status:** Draft
**Date:** 21 October 2026
**Binding:** PR04 Requirements wins on behavior. This file wins on the slice.

**Goal:** The three verbs, through FileIo, with progress.

**Not:** A queue.

---

## Decision

| Call | Why |
|---|---|
| FileIo owns the copy | The host is chrome. |
| Synchronize is a composition | FileIo has no Synchronize verb. Copy with Skip, plus a differ check. |
| Confirm Mirror | It deletes. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR04.001 | Guards. Source exists. Destination can be created. Destination is not inside the source. Paths differ. | |
| 2 | PR04.002 | Copy. FileIoHelper.Copy. Collision choice. No purge. | |
| 3 | PR04.003 | Mirror. FileIoHelper.Mirror with Purge. Confirm before a live run. | |
| 4 | PR04.004 | Synchronize. Differ by size and last write time. Optional content compare. Copy what differs. Never purge. | |
| 5 | PR04.005 | Audit Mode. Same recon. No disk change. | |
| 6 | PR04.006 | Progress in the status area. Pause, resume, cancel. Cancel deletes only files this job created. | |
| 7 | PR04.007 | Job options. Masks, size, age, depth, empty directories, timestamps, retry. | |
| 8 | PR04.008 | Log job started, job finished, job cancelled. | |
