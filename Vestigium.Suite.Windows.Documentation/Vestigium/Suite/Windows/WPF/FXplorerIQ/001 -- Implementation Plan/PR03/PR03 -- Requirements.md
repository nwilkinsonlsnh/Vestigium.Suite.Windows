# PR03 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PR03-REQ
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**Status:** Live
**Date:** 21 October 2026

**One sentence:** An Explorer can be split into two panes, each with its own location.

**This version is not** a job. The two panes can be marked source and destination, but nothing is copied yet.

---

## Must

### R03-01 Split

An Explorer can be single or split. Split shows two panes side by side. Each pane has its own location. The two can show two different folders at once.

### R03-02 Close split

Closing the split returns the Explorer to one pane and keeps the location of the pane that remains.

### R03-03 Marks

In a split, the operator can mark one pane as the source and the other as the destination. The job bar shows both paths. No job runs in this PR.

### R03-04 Remembered

The session now also stores the second location, and which pane was the source.

---

## Not in this PR

A running job. A queue.
