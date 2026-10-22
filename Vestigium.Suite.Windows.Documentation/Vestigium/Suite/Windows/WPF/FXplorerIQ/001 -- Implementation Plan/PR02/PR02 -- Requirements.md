# PR02 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PR02-REQ
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**Status:** Live
**Date:** 21 October 2026

**One sentence:** Open any number of Explorers, name them, find the ones that do not fit, and have them come back next time.

**This version is not** a split. Not a job.

---

## Must

### R02-01 More Explorers

A **+** on the tab bar opens another Explorer. There is no fixed limit. Each keeps its own location. Closing one does not close the others.

### R02-02 Tabs

A tab is a radio button with the theme underline. Double-click a tab and a small menu appears so it can be named. A tab with no name shows the folder it is in.

### R02-03 Overflow

Dashboard, Settings, and Help stay pinned on the right. When the tabs no longer fit, the ones that would be clipped collapse into an overflow immediately left of Dashboard. The overflow opens a list. Choosing one selects that Explorer.

### R02-04 Remembered

On close, the host writes every open Explorer: the name, the location, and the order. On the next open, those Explorers are restored. A path that is gone is kept and reported. The Explorer still opens.

The record is under `C:\ProgramData\Vestigium\Settings\FXplorerIQ`.

---

## Not in this PR

A split. A job. A queue.
