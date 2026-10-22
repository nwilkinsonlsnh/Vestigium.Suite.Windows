# FXplorerIQ — PR05 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PLAN-PR05
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**APPID:** `FXplorerIQ`
**Status:** Draft
**Date:** 21 October 2026
**Binding:** PR05 Requirements wins on behavior. This file wins on the slice.

**Goal:** A queue and a status area that shows it.

**Not:** Two jobs at once.

---

## Decision

| Call | Why |
|---|---|
| One at a time | The first release said so. |
| Queue is in memory | A queued job has not run. The history is a later PR. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR05.001 | JobQueue. Waiting jobs and one running job. | |
| 2 | PR05.002 | Queue action on the job bar. Run queues when a job is already running. | |
| 3 | PR05.003 | Status area lists the queue in order and shows the running job. | |
| 4 | PR05.004 | Reorder and remove. Remove does not touch the disk. | |
| 5 | PR05.005 | When the running job ends, start the next. Log job queued. | |
