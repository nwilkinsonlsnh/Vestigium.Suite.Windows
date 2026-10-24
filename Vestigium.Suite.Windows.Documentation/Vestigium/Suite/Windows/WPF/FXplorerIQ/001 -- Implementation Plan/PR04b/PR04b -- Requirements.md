# PR04b — Requirements

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PR04B-REQ
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**Status:** Live
**Date:** 24 October 2026

**One sentence:** When a job cannot run as the user, the host asks once, User Account Control runs once, and the same application finishes that job elevated.

**This version is not** a second executable, a pipe, a resident worker, or a tree scan.

---

## Must

### R04b-01 One check, not a walk

Before a Copy, Move, Delete, Mirror, or their verify forms start, the host checks the source and the destination.

- The check is an access probe on those paths. It does not walk the tree.
- A destination that does not exist, under a parent the user can write, is created. That is not elevation.
- Access denied on the source or the destination is elevation. Creating a folder on `C:\` is this case.
- An invalid path is reported. It is not elevation.

### R04b-02 The host stays a normal process

FXplorerIQ is never restarted elevated for browsing. Elevation is for one job.

### R04b-03 One ask, one UAC, this job

When the check says elevation is required:

- A themed window opens. It is not a `MessageBox`.
- Title: Elevation required.
- It names the job and the path, and says the job will run in its own elevated window.
- Continue starts User Account Control for this job. Cancel stops the job.
- If the operator denies UAC, the job stops. They are told once.

The next job that needs admin asks again. There is no approve-once-for-the-session.

### R04b-04 Same application, job mode

Continue launches this executable, not a sibling.

- `FXplorerIQ.exe --job <file>`
- The elevated instance does not open the explorer.
- It opens the same job window, runs the same FileIo job, and exits when the job ends.
- The whole job runs elevated. Every file. A hundred files that need admin are one elevated run, not a prompt per file.

### R04b-05 Progress stays ours

The elevated instance shows the existing job window. Theme, bins, live files, pause, resume, cancel, and the dashboard are the ones the host already has.

The host does not paint that progress. It says the job is running elevated, waits for the process, then refreshes the panes. A result file is enough. There is no pipe.

### R04b-06 No dead handoff

The host does not look for `FXplorerIQ.FileOps.exe`. That project is not part of this PR. The stock elevation `MessageBox` is removed.

---

## Not in this PR

A sibling executable. A named pipe. A worker that stays up. A tree preflight. Elevating the host. Changing collision, verify, or shred.
