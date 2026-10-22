# FXplorerIQ — PR06 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PLAN-PR06
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**APPID:** `FXplorerIQ`
**Status:** Draft
**Date:** 21 October 2026
**Binding:** PR06 Requirements wins on behavior. This file wins on the slice.

**Goal:** History, settings, help, and a dashboard. The first release is complete.

**Not:** Elevation. A scheduler.

---

## Decision

| Call | Why |
|---|---|
| One file per job | Same reason as a snapshot. Easy to list and easy to read. |
| Settings with the session | One folder under ProgramData. |
| Dashboard is a reading | It does not run work. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR06.001 | JobStore. One JSON file per completed job, under ProgramData\\Vestigium\\FXplorerIQ\\Jobs. | |
| 2 | PR06.002 | Jobs view. List, and a detail for the selected row. | |
| 3 | PR06.003 | Settings store. Theme, default verb, default collision, retry, Audit default, split default. | |
| 4 | PR06.004 | Settings view. Changing the theme calls ThemeManager.SwitchTheme. | |
| 5 | PR06.005 | Help. Explorers, split, verbs, collision, Audit, queue, cancel. | |
| 6 | PR06.006 | Dashboard. Counts of ran, failed, and bytes moved. No job is started from here. | |
