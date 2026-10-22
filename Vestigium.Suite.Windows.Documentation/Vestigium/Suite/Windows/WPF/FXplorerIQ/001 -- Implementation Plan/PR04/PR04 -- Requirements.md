# PR04 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PR04-REQ
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**Status:** Live
**Date:** 21 October 2026

**One sentence:** Run Copy, Mirror, and Synchronize through FileIo, with Audit Mode, and see the job move.

**This version is not** a queue. One job at a time, started by hand.

---

## Must

### R04-01 Verbs

Copy, Mirror, and Synchronize. All three take the marked source and destination. All three go through FileIo. The host does not spawn robocopy.

- Copy does not delete destination files. Collision is UniqueName, Skip, or Overwrite. UniqueName is the default.
- Mirror makes the destination match the source, including purge. A live Mirror asks first.
- Synchronize copies what is new or different and does not delete. Different is size or last write time. Content compare is optional and uses Hashing.

### R04-02 Guards

The host refuses a job whose destination is inside the source, whose source is missing, or whose source and destination are the same path.

### R04-03 Audit

Audit Mode runs the same recon and changes no disk.

### R04-04 Progress

The status area shows phase, files found, files done, files skipped, files failed, and bytes. Pause, resume, and cancel work. Cancel deletes only files this job created.

### R04-05 Options

Masks, size, age, depth, empty directories, timestamps, and retry can be set on the job. Defaults come from Settings in a later PR. This PR uses the FileIo defaults.

---

## Not in this PR

A queue. A jobs history. Settings that persist.
