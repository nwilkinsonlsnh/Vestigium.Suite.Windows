# PR05 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PR05-REQ
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**Status:** Live
**Date:** 21 October 2026

**One sentence:** A queue. Work waits, one job runs, and the status area shows both.

**This version is not** two jobs at once.

---

## Must

### R05-01 Queue

A job can be queued instead of started. The queue is ordered. The operator can reorder it and can remove a queued job. Removing a queued job does not touch the disk.

### R05-02 One at a time

One job runs. When it ends, the next queued job starts. Run, while a job is running, queues the new one.

### R05-03 Status area

The status area shows the queue and the running job. The running job shows phase, files, and bytes.

---

## Not in this PR

A jobs history. Settings. Help.
