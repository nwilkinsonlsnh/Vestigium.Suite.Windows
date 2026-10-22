# PR06 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PR06-REQ
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**Status:** Live
**Date:** 21 October 2026

**One sentence:** A jobs history, settings that persist, help, and a dashboard.

**This version closes the first release.**

---

## Must

### R06-01 Jobs

The Jobs view lists jobs this host has run. A row shows the verb, the source, the destination, the time, and the outcome. Selecting a row shows the counts and the failures. The record is under `C:\ProgramData\Vestigium\FXplorerIQ\Jobs`.

### R06-02 Settings

Settings holds the theme, the default verb, the default collision, the default retry, whether Audit Mode is the default, and whether a new Explorer opens split. Choices survive a restart, under `C:\ProgramData\Vestigium\Settings\FXplorerIQ`.

### R06-03 Help

Help explains the Explorers, the split, the three verbs, the collision choices, Audit Mode, the queue, and what cancel does. It states that the engine is FileIo.

### R06-04 Dashboard

The Dashboard reads the jobs. It shows how many ran, how many failed, and the bytes moved. It does not run a job.

---

## Not in this PR

Elevation. A scheduler. Two-way synchronize.
