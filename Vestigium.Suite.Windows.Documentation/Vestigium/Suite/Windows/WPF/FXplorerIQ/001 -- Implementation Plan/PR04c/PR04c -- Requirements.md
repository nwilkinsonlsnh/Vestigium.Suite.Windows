# PR04c — Requirements

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PR04C-REQ
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**Status:** Live
**Date:** 24 October 2026

**One sentence:** Delete (Secure) overwrites a file with cryptographic random bits, then a zero pass, then deletes it, and the Help says exactly what that does and does not do.

**This version is not** a DoD wipe, a sector erase, or a guarantee on an SSD.

---

## Must

### R04c-01 Crypto random, then zero

Delete (Secure) stops using `ZeroRandomZero` and `Random.Shared`.

- Random passes are filled from `RandomNumberGenerator`.
- A zero pass always follows.
- Default is three random passes, then zero, then delete.
- Optional is seven random passes, then zero, then delete.

### R04c-02 The setting

Secure Delete has its own area in Settings.

- Random passes: 3 or 7. Default 3.
- A line under it states the passes and that a zero pass follows.
- The choice is remembered.
- Delete (Secure) uses the saved choice.

### R04c-03 The awkward files

A normal file is overwritten in place and flushed.

A compressed, sparse, or encrypted file is not. NTFS allocates new clusters on a normal overwrite and frees the old ones. Those files use the retrieval pointers to find the clusters they occupy, and those clusters are overwritten. This is the SDelete approach. A file with no extents falls back to the stream overwrite and the log says so.

### R04c-04 Name and discard

The file is renamed before it is deleted, so the name is not left sitting in the table. After the overwrite, the ranges are TRIMmed. TRIM is a request to the drive, not a confirmation.

### R04c-05 The Help tells the truth

Help has a Secure Delete section.

- What it does: overwrites the file's contents with cryptographic random bits, writes a zero pass, and deletes the file.
- Spinning disk: that overwrite lands on the sectors the file occupied.
- SSD: the drive may keep the old data until it erases it itself. There is no guarantee.
- This is not a DoD wipe. A whole-drive sanitize, or destruction of the chips, is what clears an SSD.

---

## Not in this PR

A driver. A whole-drive sanitize. A degausser. A promise that an SSD cell is gone.
