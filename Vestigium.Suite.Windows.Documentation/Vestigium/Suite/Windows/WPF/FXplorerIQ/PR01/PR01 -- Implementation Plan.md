# FXplorerIQ — PR01 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PLAN-PR01
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**APPID:** `FXplorerIQ`
**Status:** Draft
**Date:** 21 October 2026
**Binding:** PR01 Requirements wins on behavior. This file wins on the slice.

**Goal:** A themed shell with one Explorer and one pane.

**Not:** A second Explorer. Not a job.

---

## Decision

| Call | Why |
|---|---|
| New project | FXplorerIQ is its own host, like AppMgmtIQ. |
| Logging first | The suite rule. |
| Theme before the window | So the first paint is themed. |
| One pane | The split is a later PR. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR01.001 | Create the project. WPF exe, net10.0-windows, references Themes, Logging, and FileIo. | |
| 2 | PR01.002 | Startup. Logging with APPID FXplorerIQ, FileIoCatalog.Register, then ThemeManager. | |
| 3 | PR01.003 | The window. Vestigium shell. Explorer tab on the left. Dashboard, Settings, Help pinned on the right. Status area at the bottom. | |
| 4 | PR01.004 | One Explorer, one pane. Lists a folder. Name, size, last write time. Open a folder. Open the parent. Type a path. | |
| 5 | PR01.005 | Log that the host opened. Confirm every control takes a theme style. | |
