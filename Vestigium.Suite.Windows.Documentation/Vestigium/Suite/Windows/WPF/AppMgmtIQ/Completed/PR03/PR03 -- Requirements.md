# PR03 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PR03-REQ  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**Status:** Draft  
**Date:** 19 October 2026  
**Product papers:** [Requirements_v1.0.md](../Requirements_v1.0.md), [Design_v1.0.md](../Design_v1.0.md)

This file is the **definition of done for PR03**. It adds views for common installer footprints that are not the Uninstall key itself, with KQL filtering via the shared QueryBar and a Dashboard summary.

**One sentence:** Surface App Paths, file associations / ProgIDs registered by installers, Windows Installer product information, and shared DLL reference counts so an operator can see what an application registered beyond its uninstall entry, filter the grid with the QueryBar against a Footprints KQL catalog, and surface a Dashboard section for the same data.

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

Minimum columns per section. All sections show source path and a best-effort package link when available.

| Section | Columns |
|---|---|
| App Paths | Name, Path, Scope (HKLM / Wow / HKCU), Package |
| Associations | ProgID or extension, Default, InstallSource or product code, Package |
| Installer products | Product code, Product name (when readable), Package |
| Shared DLLs | Name, Reference count, Package |

### R03-03 No writes

No controls that modify App Paths, Classes, Installer keys, or SharedDLLs.

### R03-04 QueryBar and KQL catalog

The Footprints view hosts a `VestigiumQueryBar` wired the same way as the existing views (`FilterRequested`, `Committed`, `PinRequested`, `Session`, `Queries`, `Limit`).

A Footprints field catalog lands in `Vestigium.Helpers.Kql.AppMgmt` (or a sibling) and is exposed as a session (e.g. `AppMgmtKql.Footprints()`). Minimum fields:

| Canonical | Type | Notes |
|---|---|---|
| `footprint.kind` | String | AppPath / Association / Product / SharedDll |
| `footprint.name` | String | Key name, ProgID, product code, or DLL name |
| `footprint.path` | String | Source path or value |
| `footprint.scope` | String | HKLM / Wow / HKCU when applicable |
| `footprint.refcount` | Integer | SharedDLLs only; blank otherwise |
| `footprint.package` | String | Linked package name, blank if none |

Live filter on typing. Commit records MRU only when the query binds. Same `QueryMru` limits and sticky behavior as the other views.

### R03-05 Dashboard Footprints section

The Dashboard gains a **Footprints** radio section. It binds to the same Footprints collection.

Cards (minimum): App Paths count, SharedDLLs with refcount > 1, installer products found, total footprint rows.

Charts (minimum): breakdown by kind, SharedDLL refcount distribution or top shared, product presence.

Story text updated.

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
5. QueryBar filters the grid against the Footprints catalog. Only successfully bound queries are recorded in MRU.
6. Dashboard Footprints section shows cards and charts that match the grid counts.
7. No write controls.
8. Existing tabs remain unchanged.

---

## Decision

| Call | Why |
|---|---|
| Read-only | Inventory of side effects, not a registry editor. |
| Best-effort linkage | Same approach as PR02. |
| Separate tab | Keeps Uninstall grid focused. |
| QueryBar + catalog in this PR | Same filter experience as existing tabs. |
| Dashboard section in this PR | Summary stays in sync with the new tab. |

### Rejected

| Idea | Why out |
|---|---|
| Full Classes walk | Too large and noisy. Only installer-linked subset. |
| Edit SharedDLL count | Dangerous. Out. |
| Dedicated Dashboard PR later | Dashboard would lag the data. |
| Host-local field list | Breaks the shared KQL pattern. |

---

## Parked

- Full COM / TypeLib
- Environment variables
- Repair from this view (see PR04)
