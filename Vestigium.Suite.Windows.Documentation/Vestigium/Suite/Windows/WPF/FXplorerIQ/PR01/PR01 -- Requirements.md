# PR01 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PR01-REQ
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**Status:** Live
**Date:** 21 October 2026
**Product papers:** [Requirements_v1.0.md](../../Requirements_v1.0.md), [Design_v1.0.md](../../Design_v1.0.md)

**One sentence:** A themed shell that opens with one Explorer, one pane, and a listing of a folder.

**This version is not** a second Explorer. Not a split. Not a job. Not a queue.

---

## Must

### R01-01 Project

A WPF exe, `net10.0-windows`, named `Vestigium.Suite.Windows.FXplorerIQ`. It is a host. It does not copy a file.

### R01-02 Logging

`HostLog.Initialize` runs first, with APPID `FXplorerIQ`. `FileIoCatalog.Register` runs on the same configuration. The host logs that it opened.

### R01-03 Theme

`ThemeManager` is applied before the window parses. Every surface uses a Vestigium.Themes style. A control that does not is a defect.

### R01-04 Shell

`FXplorerIqWindow` hosts a Vestigium shell. The top bar has the Explorer tab on the left and Dashboard, Settings, and Help pinned on the right. The status area is docked at the bottom.

### R01-05 One Explorer

The window opens with one Explorer. It shows one pane. The pane lists the folder it is pointed at: name, size, last write time. A folder can be opened. A parent can be opened. The path can be typed.

---

## Not in this PR

A second Explorer. A split. A job. A queue. Settings that persist. Help text.
