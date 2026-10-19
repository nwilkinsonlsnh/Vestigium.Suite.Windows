# PR03 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PR03-REQ  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**Status:** Draft  
**Date:** 19 October 2026  
**Product papers:** [Requirements_v1.0.md](../Requirements_v1.0.md), [Design_v1.0.md](../Design_v1.0.md)

This file is the **definition of done for PR03**. It adds views for common installer footprints that are not the Uninstall key itself.

**One sentence:** Surface App Paths, file associations / ProgIDs registered by installers, Windows Installer product information, and shared DLL reference counts so an operator can see what an application registered beyond its uninstall entry.

**This version is not** a full COM explorer. Not a type-library browser. Not an environment-variable editor. Read-only inventory of high-value installer side effects.

---

## Inventory sources

### App Paths

- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths`
- `HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths`

### File associations / ProgIDs (installer-registered subset)

- `HKLM\SOFTWARE\Classes` and `HKCU\Software\Classes` ProgIDs that contain an `InstallSource` or can be linked to a product code or package.
- File extension defaults that point at those ProgIDs.

### Windows Installer

- Product codes under `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData` (and related) when present.
- Cached MSI information when the Helper can surface it without launching msiexec.

### Shared DLLs

- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDLLs` reference counts.

---

## Must change

### R03-01 Footprints page or tab

New tab **Footprints** (or sub-pages) with sections or filters for:

- App Paths
- Associations
- Installer products
- Shared DLLs

Each section is a read-only grid. Single selection.

### R03-02 Columns

Minimum columns per section are defined in the Implementation Plan. All sections show source path and a best-effort package link when available.

### R03-03 No writes

No controls that modify App Paths, Classes, Installer keys, or SharedDLLs.

---

## Must not change in PR03

- Full COM registration walk
- Type library inspection
- Environment variable editing
- Any write or repair action
- Startup view changes (PR02 owns that)

---

## Acceptance

1. Footprints tab lists App Paths from the listed keys.
2. Associations section shows ProgIDs or extensions that can be linked to a package.
3. Installer products section lists product codes when present.
4. Shared DLLs section lists entries with reference counts.
5. No write controls.
6. Existing tabs remain unchanged.

---

## Decision

| Call | Why |
|---|---|
| Read-only | Inventory of side effects, not a registry editor. |
| Best-effort linkage | Same approach as PR02. |
| Separate tab | Keeps Uninstall grid focused. |

### Rejected

| Idea | Why out |
|---|---|
| Full Classes walk | Too large and noisy. Only installer-linked subset. |
| Edit SharedDLL count | Dangerous. Out. |

---

## Parked

- Full COM / TypeLib
- Environment variables
- Repair from this view (see PR04)
