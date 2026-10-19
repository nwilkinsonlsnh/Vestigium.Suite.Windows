# PR01 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PR01-REQ  
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**Status:** Live  
**Date:** 16 October 2026  
**Product papers:** [Requirements_v1.0.md](../Requirements_v1.0.md), [Design_v1.0.md](../Design_v1.0.md)

This file is the **definition of done for PR01**. It is the first release of AppMgmtIQ.

**One sentence:** List every installed package from the four classic ARP registry roots in a grid, single-select, and open an organized multi-tab details window that shows every registry value for the selected package plus computed isX64 and isGlobal.

**This version is not** an uninstaller. Not a store-app viewer. Not winget. Not bulk. Not repair/modify. Not export. The host is chrome only.

---

## Inventory sources

The Helper (`Vestigium.Helpers.WinReg` 1.0.0) enumerates these keys and their subkeys:

- `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\`
- `HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall`
- `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall`
- `HKEY_CURRENT_USER\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall`

Packages with no DisplayName are still listed. Key name is the fallback display text.

---

## Must change

### R01-01 Grid of all packages

Main view is a read-only grid bound to the Helper inventory.

Columns (minimum):

| Column | Source |
|---|---|
| DisplayName | Registry or key-name fallback |
| Publisher | Registry |
| DisplayVersion | Registry |
| InstallDate | Registry |
| EstimatedSize | Registry (display as human-readable) |
| isX64 | Computed by Helper |
| isGlobal | Computed by Helper |
| UninstallString | Registry (may be empty) |

Single selection only. No multi-select.

### R01-02 Details window

Double-click a row opens the details window.

- Title: `DisplayName (DisplayVersion)`. Updates if the selection changes while the window is open.
- Content: multiple tabs that organize every registry value present for that package.
- Computed fields isX64 and isGlobal appear in both the grid and the details.

isX64: true when the package is not under a Wow6432Node key (x64). false = x86.  
isGlobal: true when the package is under HKLM. false = HKCU.

### R01-03 Chrome

Suite chrome: `AppMgmtIqWindow` + `VestigiumShell`, ThemeManager on startup, StatusBar, single page for this release.

Startup order: HostLog.Initialize(AppMgmtIQ) → Themes → DI → window.

### R01-04 Tests

Host tests stay off the registry. No live walk in the test project. Helper owns the inventory tests.

---

## Must not change in PR01

- Uninstall launch
- Store apps / winget
- Bulk, repair, modify, export
- Anything outside the four registry roots
- A second façade inside the host

---

## Acceptance

1. Window opens with suite chrome. Grid fills with packages from all four roots.
2. Single selection only.
3. Double-click opens details. Title is DisplayName (DisplayVersion).
4. Details shows every registry value for the package, organized in tabs.
5. isX64 and isGlobal appear and match the source key (Wow6432Node → false, HKLM → true).
6. No uninstall button. No store apps. No winget.
7. Host tests stay green and do not open the registry.

---

## Decision

| Call | Why |
|---|---|
| Thin host | Suite rule. Logic stays in WinReg. |
| Single selection | Requirements lock. |
| Details on double-click | Requirements lock. |
| All four roots | Requirements lock. |
| No uninstall in this PR | Explicitly out of first release. |

### Rejected

| Idea | Why out |
|---|---|
| Uninstall button “for later” | Out of release. Do not leave a dead control. |
| Multi-select | Requirements say one package at a time. |
| Store apps in the same grid | Different store. Different PR. |
| Host walks the registry itself | Forbidden façade. |

---

## Parked (not required to close PR01)

- Uninstall launch (confirmed)
- Search / filter
- Column formatting polish (size units, date parse)
- Settings / theme persist
- Export
- Store apps
- winget
