# PR04 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PR04-REQ
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**Status:** Live
**Date:** 22 October 2026

**One sentence:** Run Copy, Move, Delete, and Mirror through FileIo, with Verify where it is asked, and see the job move.

**This version is not** a queue. One job at a time, started by hand.

---

## Must

### R04-01 Verbs

The verbs live on the Explorer context menu. There is no button bar for them. Each verb goes through FileIo. The host does not spawn robocopy.

| Menu | Does |
|---|---|
| Copy | Copy. Source stays. No purge. |
| Copy (Verify) | Copy, then verify. Source stays. |
| Move | Move. Source is removed after the write. |
| Move (Verify) | Move, then verify. Source is removed only if the verify passes. |
| Delete | Delete the selection. File or folder. |
| Delete (Secure) | Delete with a shred recipe, then delete. |
| Mirror (Verify) | Mirror with purge, then verify. Always verified. |

Copy, Move, and Mirror take the marked source and destination. If there is no destination mark, the menu asks for one before the job opens. Delete takes the selection only.

- Collision is UniqueName, Skip, or Overwrite. UniqueName is the default. The default comes from Settings.
- A live Mirror asks first. The confirm says it will purge.
- Mirror is always verified. There is no Mirror without Verify. The menu says Mirror (Verify) so that is plain.

### R04-02 Guards

The host refuses a job whose destination is inside the source, whose source is missing, or whose source and destination are the same path. Delete has no destination guard.

### R04-03 Verify

Verify is ALCOA+. FileIo compares the two files through Hashing and returns both digests. The operation log records them.

- Attributable. Who ran the job.
- Contemporaneous. The verify line is written when the compare finishes.
- Original. The source hash.
- Accurate. The destination hash, taken after the write. Equal or not.
- Complete. Every file has a verify line, or a failed line. A skipped file says why.
- Enduring. The operation log is the record.

Copy (Verify) copies, then compares. The source stays. Move (Verify) copies, compares, and deletes the source only if the digests match. A failed verify leaves the source and marks the file failed. Mirror (Verify) verifies after the mirror, including the purge.

The verify algorithm is the one chosen on the Collision tab. SHA-256 is the default.

### R04-04 Collision settings

Settings has a Collision tab. It holds the defaults a job uses.

- Collision. UniqueName, Skip, or Overwrite. UniqueName starts selected.
- UniqueName pattern. The FileIo pattern, shown.
- Ask before Overwrite. On by default.
- Verify algorithm. The Hashing list. SHA-256 starts selected.

The job window reads these when it opens. The operator can override for that job. The override does not write back.

### R04-05 Audit

Audit Mode runs the same recon and changes no disk. The bins still fill. The verb reads as it would have run.

### R04-06 Progress

The job is a themed modal, not a message box. It is resizable. The size is remembered.

- Phase. Recon, Transfer, Finalize, Done.
- Overall. Bytes, files found, done, skipped, failed. Throughput and an ETA once recon is far enough.
- Current file. Its own row and its own bar.
- Grid. Name, bucket, size, status, rate. The row in flight is marked. A failure stays and shows the reason.
- Bins. Tiny, Small, Medium, Large, Huge. They fill during recon and drain during transfer. A bin closes when it is empty.

Pause, resume, and cancel work. Pause finishes the current buffer. Cancel deletes only files this job created, and the confirm says so.

### R04-07 Access

The host does not assume admin. A probe checks read on the source and write on the destination before the job starts.

- If the probe passes, the job runs in process.
- If it fails with access denied, the operator is offered elevation. The offer says which path and which right.
- Audit never elevates.
- The main window is never elevated. An elevated job runs in `FXplorerIQ.FileOps`, one UAC per job. A cancelled UAC is a status.

### R04-08 Operation log

Each job has one correlation id. It is the FileIo job id, `fio-` plus twelve hex.

The operation log is its own file.

`C:\ProgramData\Vestigium\FXplorerIQ\Operations\{correlation}.jsonl`

The main log gets a start line and an end line. Each carries the verb, the correlation id, the operation log path, and the source. Copy, Move, and Mirror also carry the destination. Delete carries the source only.

FileIo's events for that job go to the operation file. The main log is the table of contents.

### R04-09 Options

Masks, size, age, depth, empty directories, timestamps, and retry can be set on the job. Defaults other than collision come from FileIo in this PR.

---

## Not in this PR

A queue. A jobs history. Two jobs at once. Elevating the Explorer itself.
