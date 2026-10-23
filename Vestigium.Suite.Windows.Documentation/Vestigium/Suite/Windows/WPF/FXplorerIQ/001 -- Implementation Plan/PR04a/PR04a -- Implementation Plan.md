# FXplorerIQ — PR04a Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PLAN-PR04A
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**APPID:** `FXplorerIQ`
**Status:** Draft
**Date:** 22 October 2026
**Binding:** PR04a Requirements wins on behavior. This file wins on the slice.

**Goal:** No job starts without a confirmed destination. The window is themed and shows the files in flight.

**Not:** A new verb.

---

## Decision

| Call | Why |
|---|---|
| Confirm before the job | The other pane is a hint, not a destination. |
| WPF picker, not Win32 | It has to take the theme. |
| Theme the window | The system chrome is the gap in the screenshot. |
| Live list is a library change | FileIoProgress has bins, not files. The host does not invent them. |
| Register the catalogs | 10510 is Analytics. FileIo is unregistered too. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR04a.001 | Register FileIoCatalog and AnalyticsCatalog at startup. A log miss never becomes the job status. | Done |
| 2 | PR04a.002 | Confirm sheet. Source read only. Destination editable. OK starts. Cancel does nothing. Prefill from the other pane, never auto-start. | Done |
| 3 | PR04a.003 | Themed folder picker. This PC, drives, folders. Browse on the confirm sheet. | |
| 4 | PR04a.004 | Theme the job window. Source and destination stay visible. Pause, resume, cancel use the theme. | |
| 5 | PR04a.005 | FileIo exposes the live files. Publish. Bump the host. Grid of name, bucket, size, bar, status. Finished rows leave. Failed rows stay. | |
