# FXplorerIQ — PR04c Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PLAN-PR04C
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**APPID:** `FXplorerIQ`
**Status:** Draft
**Date:** 24 October 2026
**Binding:** PR04c Requirements wins on behavior. This file wins on the slice.

**Goal:** Delete (Secure) does the best a file tool can do, and the Help does not overclaim.

**Not:** A DoD wipe. A driver. A whole-drive erase.

---

## Decision

| Call | Why |
|---|---|
| Three random, then zero | That is the agreed default. Seven is the slow option. |
| Crypto generator | `Random.Shared` is not a cryptographic fill. |
| Pointers only for the awkward files | A normal overwrite already hits the clusters. Compressed, sparse, and encrypted do not. |
| Rename, then TRIM | The name should not remain, and TRIM is a request we can make. |
| Honest Help | An SSD does not offer per-file erase. The page must say so. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR04c.001 | FileIo fill uses `RandomNumberGenerator`. Publish. | Done |
| 2 | PR04c.002 | Secure Delete setting. 3 or 7, default 3, remembered. Delete (Secure) uses it. | Done |
| 3 | PR04c.003 | Retrieval pointers for compressed, sparse, and encrypted files. Overwrite those clusters. Log the fallback. | Done |
| 4 | PR04c.004 | Rename before delete. TRIM the ranges after the overwrite. | |
| 5 | PR04c.005 | Help section. What it does, spinning disk, SSD, not a DoD wipe. | |
