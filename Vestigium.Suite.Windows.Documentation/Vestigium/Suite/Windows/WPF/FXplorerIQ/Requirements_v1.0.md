# FXplorerIQ — Requirements v1.0

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-SRS-001
**Version:** 1.0
**Status:** Initial lock
**Date:** 21 October 2026
**Project:** Vestigium.Suite.Windows.FXplorerIQ
**Kind:** WPF, net10.0-windows, MVVM

This is the first lock for FXplorerIQ. It states what the first release does. It follows the same frame as AppMgmtIQ: a thin host, shared work in a helper, elevation only when a write requires it.

---

## 1. Purpose

FXplorerIQ is a tabbed file explorer for moving work between two places. An operator sets a source and a destination, sees both, and runs a file job. The jobs are Copy, Mirror, and Synchronize.

The engine is `Vestigium.Helpers.FileIo`. The host does not spawn `robocopy.exe` and does not re-implement the copy. Robocopy is the behavior reference. FileIo is the record.

---

## 2. First release

The first release includes:

- A shell in the Vestigium frame, with theme, status bar, settings, and help.
- One or more work tabs. Each tab is one job: a source, a destination, and a verb.
- A browser for each side, so the operator can see the folders before the job runs.
- Copy, Mirror, and Synchronize, run through FileIo.
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

Explorer holds the work tabs. Jobs holds the history of jobs this host has run. Settings holds the theme and the job defaults. Help explains the verbs and the options.

---

## 4. Work tab

A work tab has two panes and a job bar.

- The left pane is the source. The right pane is the destination.
- Each pane shows the path, and the folders and files under it.
- The operator can browse either pane, and can set the current folder as the source or the destination of the job.
- A tab remembers its source, its destination, and its verb.

More than one tab may be open. Each tab is independent. Closing a tab does not cancel a job that is running. The job moves to the Jobs list.

---

## 5. Verbs

All three verbs take a source directory and a destination directory. All three go through FileIo. All three can run in Audit Mode.

### 5.1 Copy

Copy reads the source and writes what it finds to the destination. It does not delete anything that is already in the destination.

Collision is a choice:

- UniqueName, the default. A name that exists gets a numbered suffix. The original is never overwritten. The cap is an error, not a clobber.
- Skip. An existing name is left alone.
- Overwrite. An existing name is replaced.

### 5.2 Mirror

Mirror makes the destination match the source. Files in the source are copied. Files in the destination that are not in the source are removed. This is FileIo Mirror with purge.

Mirror is the destructive verb. The host asks for confirmation before a live Mirror. Audit Mode does not ask, because it changes nothing.

### 5.3 Synchronize

Synchronize is one way. It copies files that are new or different, and it does not delete destination files that the source does not have.

A file is different when the size or the last write time differs. When the operator asks for content compare, the difference is the digest from `Vestigium.Helpers.Hashing`.

Synchronize is a host composition. It uses FileIo Copy with Skip for names that match, and Copy for names that differ. It does not use Mirror.

---

## 6. Job options

A job can set:

- Include or exclude file masks, and exclude directory masks.
- Minimum and maximum size.
- Minimum and maximum age.
- Maximum depth.
- Whether empty directories are included.
- Whether timestamps and attributes are copied.
- Retry count and retry wait.
- Audit Mode.

Defaults come from Settings. A tab can override them for one job.

---

## 7. Running a job

1. The operator sets source and destination.
2. The host refuses a job whose destination is inside the source.
3. Audit Mode runs the same recon and writes the decisions. It changes no disk.
4. A live job runs through FileIo. Progress reports phase, files found, files done, files skipped, files failed, and bytes.
5. Pause finishes the current buffer and waits. Resume continues. Cancel stops the job and deletes only files this job created.
6. The result lists copied, skipped, failed, and, for Mirror, removed.
7. The job is written to the Jobs list.

---

## 8. Jobs list

The Jobs tab lists jobs this host has run. A row shows the verb, the source, the destination, the time, and the outcome. Selecting a row shows the counts and the failures.

The list is kept under `C:\ProgramData\Vestigium\FXplorerIQ\Jobs`.

---

## 9. Settings

Settings holds:

- The theme.
- The default verb.
- The default collision.
- The default retry count and wait.
- Whether Audit Mode is the default.

Choices are written under `C:\ProgramData\Vestigium\Settings\FXplorerIQ`.

---

## 10. Help

Help explains the three verbs, the collision choices, Audit Mode, and what cancel does. It states that the engine is FileIo and that the host does not call robocopy.

---

## 11. Elevation

Reading a folder the operator can already read needs no elevation. A job that fails on access is reported as a failure, not silently retried as administrator.

A later release may add an elevated worker for a path the operator cannot write. The first release does not elevate.

---

## 12. Logging

The host logs with APPID `FXplorerIQ` through Vestigium.Logging. FileIo logs under the host APPID, with its own correlation id on each job. The host registers `FileIoCatalog` at startup. The library does not initialize the logger.

---

## 13. Out of the first release

- Two-way synchronize.
- A scheduled job.
- Search across drives.
- An elevated worker.
- Secure delete.
- Copy of security descriptors or alternate data streams.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 21 Oct 2026 | Initial lock. Tabbed explorer. Copy, Mirror, Synchronize through FileIo. |
