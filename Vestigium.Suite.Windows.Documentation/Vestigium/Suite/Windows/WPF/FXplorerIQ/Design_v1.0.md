# FXplorerIQ — Design v1.0

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-SDD-001
**Version:** 1.0
**Status:** Initial lock
**Date:** 21 October 2026
**Project:** Vestigium.Suite.Windows.FXplorerIQ

This document describes how the first release is built. It follows the AppMgmtIQ frame: a thin host, the work in a helper, the window in the Vestigium shell.

---

## 1. Boundary

| Layer | Owns |
|---|---|
| Host (`FXplorerIQ`) | Window, tabs, panes, job bar, settings, help, job history. |
| Helper (`Vestigium.Helpers.FileIo`) | Recon, copy, mirror, collision, pause, cancel, progress, audit. |
| Helper (`Vestigium.Helpers.Hashing`) | Digest, when Synchronize compares content. |
| Helper (`Vestigium.Helpers.FileSystemPaths`) | Path cleaning and tokens, when a path is entered. |

The host does not spawn `robocopy.exe`. It does not read a payload into memory. FileIo owns the streams.

---

## 2. Projects

| Project | Kind |
|---|---|
| Vestigium.Suite.Windows.FXplorerIQ | WPF exe, net10.0-windows. |

No worker in the first release. Elevation is out of scope. A later release can add `FXplorerIQ.Copy` the same way AppMgmtIQ added its workers.

---

## 3. Startup

1. `HostLog.Initialize` with APPID `FXplorerIQ`.
2. `FileIoCatalog.Register` on the same configuration.
3. ThemeManager, before the window parses.
4. The shell, the view models, and the views.

---

## 4. Shell

`FXplorerIqWindow` hosts a Vestigium shell. Navigation items are Explorer, Jobs, Settings, and Help. The status bar is the shell status bar. A running job reports into it.

---

## 5. Explorer

### 5.1 Tabs

A tab is a `WorkTab`. It holds a source path, a destination path, a verb, and the option overrides. Tabs are an observable collection. The selected tab is the one the panes show.

### 5.2 Panes

Each pane is a directory listing. It binds to a path. Changing the path re-reads the directory. The listing shows name, size, and last write time. A folder can be opened. A parent can be opened.

The listing is a view model. It does not copy. It only reads the directory the operator can already read.

### 5.3 Job bar

The job bar shows the source, the destination, the verb, and the actions: Audit, Run, Pause, Resume, Cancel.

Run builds a `FileIoJob` from the tab and calls `RunAsync`. Progress is bound to the job's `IProgress<FileIoProgress>`.

---

## 6. Verbs

| Verb | FileIo | Host rule |
|---|---|---|
| Copy | `FileIoHelper.Copy` | No purge. Collision from the tab. |
| Mirror | `FileIoHelper.Mirror` with `Purge` | Confirm before a live run. |
| Synchronize | `FileIoHelper.Copy` | Skip names that match. Copy names that differ. Never purge. |

Synchronize decides "differ" before the copy. Size and last write time are the default. A content compare uses `CompareFiles` from FileIo, which delegates to Hashing.

Audit Mode sets `FileIoJobOptions.AuditMode`. The same recon runs. No disk change.

---

## 7. Guards

Before a job starts the host checks:

- Source exists and is a directory.
- Destination exists, or can be created.
- Destination is not inside the source.
- Source and destination are not the same path.

A failed guard is reported. The job is not started.

---

## 8. Progress and cancel

FileIo reports phase, files, and bytes. The host shows that on the job bar and in the status bar.

Pause calls `job.Pause`. Resume calls `job.Resume`. Cancel calls `job.Cancel`. Cancel deletes only files this job created. The host does not delete anything else.

---

## 9. Jobs list

`JobStore` writes one JSON file per completed job under `C:\ProgramData\Vestigium\FXplorerIQ\Jobs`. A file holds the verb, the paths, the time, the counts, and the failures. It does not hold file contents.

The Jobs view lists those files. Selecting one shows the record.

---

## 10. Settings

`FXplorerSettingsStore` reads and writes JSON under `C:\ProgramData\Vestigium\Settings\FXplorerIQ`.

Settings holds the theme, the default verb, the default collision, and the default retry. Changing the theme calls `ThemeManager.SwitchTheme`.

---

## 11. Theming

The host uses Vestigium.Themes. Buttons, menus, the panes, and the job bar take their styles from the theme.

---

## 12. Logging

FileIo writes through the host logger. Each job has a correlation id, `fio-` plus 12 hex, from FileIo. The host logs the request, the outcome, and a cancelled run. Paths are logged. File contents are not.

---

## 13. What this design does not do

- It does not elevate.
- It does not schedule.
- It does not merge two ways.
- It does not index the machine.

Those are later releases. The first release is the tab, the two panes, and the three verbs.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 21 Oct 2026 | Initial lock. Thin host. FileIo engine. Copy, Mirror, Synchronize. |
