# FXplorerIQ — PR02 Implementation Plan

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PLAN-PR02
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**APPID:** `FXplorerIQ`
**Status:** Draft
**Date:** 21 October 2026
**Binding:** PR02 Requirements wins on behavior. This file wins on the slice.

**Goal:** Many Explorers, named, with an overflow, and a remembered session.

**Not:** A split. Not a job.

---

## Decision

| Call | Why |
|---|---|
| Tabs are radios | The suite underline. |
| Overflow, not a scroll | The pinned menus must stay put. |
| Remember on close | The operator leaves work open. |

---

## Implementation table

| Order | ID | Do | State |
| ---: | :--- | :--- | :--- |
| 1 | PR02.001 | A collection of Explorers. The + adds one. Closing one leaves the others. | Done |
| 2 | PR02.002 | Tabs as underlined radios. Double-click opens a naming menu. An unnamed tab shows the folder. | Done |
| 3 | PR02.003 | Overflow immediately left of Dashboard. Hidden while every tab fits. A list of the hidden tabs. Choosing one selects it. | Done |
| 4 | PR02.004 | ExplorerSession. Write name, location, and order on close. Restore on open. A missing path is kept and reported. | Done |
| 5 | PR02.005 | Log Explorer opened and Explorer closed. | |
