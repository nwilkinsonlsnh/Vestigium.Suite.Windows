# FXplorerIQ — PR03 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PLAN-PR03
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**APPID:** `FXplorerIQ`
**Status:** Draft
**Date:** 21 October 2026
**Binding:** PR03 Requirements wins on behavior. This file wins on the slice.

**Goal:** A split Explorer with two locations, and a mark for source and destination.

**Not:** A job.

---

## Decision

| Call | Why |
|---|---|
| Two panes, one Explorer | The operator asked to split the window they are in. |
| Marks, no job | Source and destination have to exist before a verb does. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR03.001 | Split command. An Explorer holds one pane or two. | |
| 2 | PR03.002 | Two panes side by side, each with its own path. Open, parent, and typed path work in both. | |
| 3 | PR03.003 | Close split. Keep the remaining pane. | |
| 4 | PR03.004 | Mark source and destination. The job bar shows both paths. | |
| 5 | PR03.005 | Session stores the second path and which pane is the source. Log split opened and split closed. | |
