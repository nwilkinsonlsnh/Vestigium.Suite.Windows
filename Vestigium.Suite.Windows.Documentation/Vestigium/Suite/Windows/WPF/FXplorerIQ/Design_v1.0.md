# FXplorerIQ — Design v1.0

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-SDD-001
**Version:** 1.0
**Status:** Initial lock
**Date:** 21 October 2026
**Project:** Vestigium.Suite.Windows.FXplorerIQ

This document describes how the first release is built. It follows the AppMgmtIQ frame: a thin host, the work in a helper, the window in the Vestigium shell.

Vestigium.Themes and Vestigium.Logging are part of the frame, not options. The window takes its look from the theme. The host writes through the logger.

---

## 1. Boundary

| Layer | Owns |
|---|---|
| Host (`FXplorerIQ`) | Window, Explorers, split panes, job bar, queue, status area, settings, help, job history. |
| Helper (`Vestigium.Helpers.FileIo`) | Recon, copy, mirror, collision, pause, cancel, progress, audit. |
| Helper (`Vestigium.Helpers.Hashing`) | Digest, when Synchronize compares content. |
| Helper (`Vestigium.Helpers.FileSystemPaths`) | Path cleaning and tokens, when a path is entered. |
| Themes (`Vestigium.Themes`) | Every surface. |
| Logging (`Vestigium.Logging`) | Every recorded action. |

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
4. The shell, one Explorer, and the status area.

---

## 4. Shell

`FXplorerIqWindow` hosts a Vestigium shell. Navigation items are Explorer, Jobs, Settings, and Help.

The Explorer surface holds the open Explorers and the status area. The status area is docked at the bottom. It is always visible while Explorer is selected.

Styles come from Vestigium.Themes. A button uses a theme button style. A menu uses a theme menu style. A splitter uses the theme stroke. A pane uses the theme surface.

---

## 5. Explorers

### 5.1 Opening

The window opens with one `ExplorerView`. Each Explorer has a **+**. The command adds another `ExplorerView` to the collection. There is no fixed limit. Each Explorer is independent.

An Explorer holds one or two `PaneView` models. Single is one pane. Split is two.

### 5.2 Panes

A pane binds to a path. Changing the path re-reads the directory. The listing shows name, size, and last write time. A folder can be opened. A parent can be opened.

The listing is a view model. It does not copy. It only reads the directory the operator can already read.

### 5.3 Split

Split replaces the single pane with two panes side by side. Each pane has its own path. Close split keeps one pane and discards the other.

The two panes are the natural source and destination. The operator marks which pane is the source.

### 5.4 Across Explorers

A job can take its source from one Explorer and its destination from another. The job bar shows both paths, wherever they came from.

---

## 6. Job bar

The job bar on the Explorer shows the source, the destination, the verb, and the actions: Queue, Audit, Run, Pause, Resume, Cancel.

Queue adds the job to the queue and does not start it. Run starts it now, or queues it if a job is already running.

---

## 7. Queue and status

`JobQueue` holds the waiting jobs and the running job. One job runs at a time.

The status area binds to the queue.

- The queued list shows verb, source, destination, in order. The operator can move a row or remove it.
- The running row shows phase, files found, files done, files skipped, files failed, and bytes, from `FileIoProgress`.

When the running job ends, the queue starts the next one.

---

## 8. Verbs

| Verb | FileIo | Host rule |
|---|---|---|
| Copy | `FileIoHelper.Copy` | No purge. Collision from the Explorer. |
| Mirror | `FileIoHelper.Mirror` with `Purge` | Confirm before a live run. |
| Synchronize | `FileIoHelper.Copy` | Skip names that match. Copy names that differ. Never purge. |

Synchronize decides "differ" before the copy. Size and last write time are the default. A content compare uses `CompareFiles` from FileIo, which delegates to Hashing.

Audit Mode sets `FileIoJobOptions.AuditMode`. The same recon runs. No disk change.

---

## 9. Guards

Before a job starts the host checks:

- Source exists and is a directory.
- Destination exists, or can be created.
- Destination is not inside the source.
- Source and destination are not the same path.

A failed guard is reported. The job is not started, and it is not left in the queue.

---

## 10. Progress and cancel

FileIo reports phase, files, and bytes. The host shows that in the status area.

Pause calls `job.Pause`. Resume calls `job.Resume`. Cancel calls `job.Cancel`. Cancel deletes only files this job created. The host does not delete anything else.

---

## 11. Jobs list

`JobStore` writes one JSON file per completed job under `C:\ProgramData\Vestigium\FXplorerIQ\Jobs`. A file holds the verb, the paths, the time, the counts, and the failures. It does not hold file contents.

The Jobs view lists those files. Selecting one shows the record.

---

## 12. Settings

`FXplorerSettingsStore` reads and writes JSON under `C:\ProgramData\Vestigium\Settings\FXplorerIQ`.

Settings holds the theme, the default verb, the default collision, the default retry, and whether a new Explorer opens split. Changing the theme calls `ThemeManager.SwitchTheme`. The open Explorers and the status area repaint from the new theme.

---

## 13. Logging

`HostLog.Initialize` runs first, with APPID `FXplorerIQ`. `FileIoCatalog.Register` runs on the same configuration.

The host logs Explorer opened, Explorer closed, split opened, split closed, job queued, job started, job finished, and job cancelled. FileIo writes its own events under the same APPID, with correlation id `fio-` plus 12 hex. Paths are logged. File contents are not.

---

## 14. What this design does not do

- It does not elevate.
- It does not schedule.
- It does not merge two ways.
- It does not index the machine.
- It does not run two jobs at once.

Those are later releases. The first release is the Explorers, the split, the queue, and the three verbs.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 21 Oct 2026 | Initial lock. Explorers with +, split panes, queue and status area, FileIo engine. |
