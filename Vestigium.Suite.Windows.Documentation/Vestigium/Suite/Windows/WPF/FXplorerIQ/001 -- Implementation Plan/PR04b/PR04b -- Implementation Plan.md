# FXplorerIQ — PR04b Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PLAN-PR04B
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**APPID:** `FXplorerIQ`
**Status:** Draft
**Date:** 24 October 2026
**Binding:** PR04b Requirements wins on behavior. This file wins on the slice.

**Goal:** A job the user cannot run is asked for once, elevated once, and finished by this same application.

**Not:** A second executable. A pipe. A resident worker. A tree scan.

---

## Decision

| Call | Why |
|---|---|
| One probe on source and destination | That is how Explorer decides. A walk is a second recon and can still be wrong. |
| Same exe, `--job` mode | A process cannot gain admin after it starts. Relaunching this exe is enough. |
| Elevated instance owns the job window | Progress, bins, and the dashboard already live there. No pipe to build. |
| UAC per job | Explorer asks per operation. A session worker was the custom idea we dropped. |
| Themed ask, no `MessageBox` | The stock boxes are the gap in the screenshot. |
| Missing writable folder is a create | Elevation is access denied, not "path does not exist." `C:\` is access denied. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR04b.001 | Access gate. Probe source and destination only. Creatable missing folder is created. Access denied is elevation. Invalid path is a message. | Done |
| 2 | PR04b.002 | Themed elevation window. Job, path, Continue, Cancel. Replaces both `MessageBox` calls. No `FileOps.exe` lookup. | Done |
| 3 | PR04b.003 | `--job` mode. Elevated instance reads the job file, opens the job window, runs FileIo, writes a result file, exits. No explorer UI. | Done |
| 4 | PR04b.004 | Host writes the job file, starts this exe with `runas`, waits, refreshes the panes from the result. UAC denied stops the job. | |
| 5 | PR04b.005 | Remove the old handoff. No sibling path. No second error box. | |
