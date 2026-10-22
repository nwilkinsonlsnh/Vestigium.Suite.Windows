# FXplorerIQ — Requirements v1.0

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-SRS-001
**Version:** 1.0
**Status:** Initial lock
**Date:** 21 October 2026
**Project:** Vestigium.Suite.Windows.FXplorerIQ
**Kind:** WPF, net10.0-windows, MVVM

This is the first lock for FXplorerIQ. It states what the first release does. It follows the same frame as AppMgmtIQ: a thin host, shared work in a helper, and the Vestigium look.

The host uses Vestigium.Themes for every surface, and Vestigium.Logging for every recorded action. Both are required. The window is an IQ suite window, not a stock explorer.

---

## 1. Purpose

FXplorerIQ is a tabbed file explorer for moving work between two places. An operator opens one or more explorers, sets a source and a destination, sees both, and runs a file job. The jobs are Copy, Mirror, and Synchronize.

The engine is `Vestigium.Helpers.FileIo`. The host does not spawn `robocopy.exe` and does not re-implement the copy. Robocopy is the behavior reference. FileIo is the record.

---

## 2. First release

The first release includes:

- A shell in the Vestigium frame, themed, with a status area, settings, and help.
- One Explorer on open, and a control to open more. Any number may stay open.
- A split on an Explorer, so one Explorer can show two locations at once.
- Copy, Mirror, and Synchronize, run through FileIo.
- A queue. Work waits, then runs, and the status area shows both.
- Audit Mode, so a job can be planned without changing the disk.
- Progress, pause, resume, and cancel.
- A result the operator can read when the job ends.

The first release does not include a two-way merge, a scheduler, or a search across the machine.

---

## 3. Shell

The window is a Vestigium shell. The navigation is:

1. Explorer
2. Jobs
3. Settings
4. Help

Explorer is the work surface. Jobs holds the history of jobs this host has run. Settings holds the theme and the job defaults. Help explains the verbs and the options.

The look comes from Vestigium.Themes. Buttons, menus, panes, splitters, and the status area use the theme. Changing the theme changes the window. A control that does not take a theme style is a defect.

---

## 4. Explorers

The window opens with one Explorer.

Each Explorer has a **+**. Choosing it opens a second Explorer, identical to the first. The operator may open as many as they need and leave them open. Each Explorer keeps its own location. Closing one does not close the others, and does not cancel work that Explorer started.

An Explorer can be shown in two ways.

- **Single.** One location, one listing.
- **Split.** The Explorer is divided into two panes. Each pane has its own location. The two panes can show two different folders at the same time.

The split can be opened and closed. Closing the split returns the Explorer to a single pane and keeps the location of the pane that remains.

A listing shows name, size, and last write time. A folder can be opened. A parent can be opened. The path can be typed or browsed.

---

## 5. Source and destination

A job needs a source and a destination.

- In a split Explorer, one pane is the source and the other is the destination. The operator chooses which is which.
- Across Explorers, the operator can take the location of one Explorer as the source and the location of another as the destination.

The job bar on the Explorer shows the source, the destination, and the verb.

---

## 6. Verbs

All three verbs take a source directory and a destination directory. All three go through FileIo. All three can run in Audit Mode.

### 6.1 Copy

Copy reads the source and writes what it finds to the destination. It does not delete anything that is already in the destination.

Collision is a choice:

- UniqueName, the default. A name that exists gets a numbered suffix. The original is never overwritten. The cap is an error, not a clobber.
- Skip. An existing name is left alone.
- Overwrite. An existing name is replaced.

### 6.2 Mirror

Mirror makes the destination match the source. Files in the source are copied. Files in the destination that are not in the source are removed. This is FileIo Mirror with purge.

Mirror is the destructive verb. The host asks for confirmation before a live Mirror. Audit Mode does not ask, because it changes nothing.

### 6.3 Synchronize

Synchronize is one way. It copies files that are new or different, and it does not delete destination files that the source does not have.

A file is different when the size or the last write time differs. When the operator asks for content compare, the difference is the digest from `Vestigium.Helpers.Hashing`.

Synchronize is a host composition. It uses FileIo Copy with Skip for names that match, and Copy for names that differ. It does not use Mirror.

---

## 7. Job options

A job can set:

- Include or exclude file masks, and exclude directory masks.
- Minimum and maximum size.
- Minimum and maximum age.
- Maximum depth.
- Whether empty directories are included.
- Whether timestamps and attributes are copied.
- Retry count and retry wait.
- Audit Mode.

Defaults come from Settings. An Explorer can override them for one job.

---

## 8. Queue and status

A job does not have to start at once. The operator can queue it.

The status area sits at the bottom of the window. It shows two things.

- **Queued.** Jobs waiting to run, in order.
- **Running.** The job in progress, with phase, files, and bytes.

One job runs at a time in the first release. When it ends, the next queued job starts. The operator can reorder the queue, remove a queued job, and pause, resume, or cancel the running job.

Cancel stops the running job and deletes only files that job created. Removing a queued job does not touch the disk.

---

## 9. Running a job

1. The operator sets source and destination.
2. The host refuses a job whose destination is inside the source.
3. Audit Mode runs the same recon and writes the decisions. It changes no disk.
4. A live job runs through FileIo. The status area reports phase, files found, files done, files skipped, files failed, and bytes.
5. Pause finishes the current buffer and waits. Resume continues. Cancel stops the job and deletes only files this job created.
6. The result lists copied, skipped, failed, and, for Mirror, removed.
7. The job is written to the Jobs list.

---

## 10. Jobs list

The Jobs tab lists jobs this host has run. A row shows the verb, the source, the destination, the time, and the outcome. Selecting a row shows the counts and the failures.

The list is kept under `C:\ProgramData\Vestigium\FXplorerIQ\Jobs`.

---

## 11. Settings

Settings holds:

- The theme.
- The default verb.
- The default collision.
- The default retry count and wait.
- Whether Audit Mode is the default.
- Whether a new Explorer opens split or single.

Choices are written under `C:\ProgramData\Vestigium\Settings\FXplorerIQ`.

---

## 12. Help

Help explains the Explorers, the split, the three verbs, the collision choices, Audit Mode, the queue, and what cancel does. It states that the engine is FileIo and that the host does not call robocopy.

---

## 13. Logging

The host logs with APPID `FXplorerIQ` through Vestigium.Logging. This is required. A host that does not initialize the logger is not this application.

FileIo logs under the host APPID, with its own correlation id on each job. The host registers `FileIoCatalog` at startup. The library does not initialize the logger.

The host logs an Explorer opened, an Explorer closed, a split opened, a job queued, a job started, a job finished, and a job cancelled. Paths are logged. File contents are not.

---

## 14. Elevation

Reading a folder the operator can already read needs no elevation. A job that fails on access is reported as a failure, not silently retried as administrator.

A later release may add an elevated worker for a path the operator cannot write. The first release does not elevate.

---

## 15. Out of the first release

- Two-way synchronize.
- A scheduled job.
- Search across drives.
- An elevated worker.
- Secure delete.
- Copy of security descriptors or alternate data streams.
- More than one job running at the same time.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 21 Oct 2026 | Initial lock. Explorers, split panes, queue, Copy, Mirror, Synchronize through FileIo. |
