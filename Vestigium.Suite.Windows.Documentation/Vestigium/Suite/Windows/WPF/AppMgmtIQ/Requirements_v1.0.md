# AppMgmtIQ — Requirements v1.0

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-SRS-001  
**Version:** 1.0  
**Status:** Draft — staging  
**Date:** 16 October 2026  
**Project:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**Kind:** WPF exe, `net10.0-windows`, MVVM  
**APPID:** `AppMgmtIQ`  
**Logic:** `Vestigium.Helpers.WinReg` (and any future AppMgmt helper)  
**Chrome:** `Vestigium.Themes`, `Vestigium.Controls`, StatusBar, NumericUpDown, UnderConstruction, Converters  
**Logging:** `Vestigium.Logging`

---

## 1. What ships

One thin host window. Lists installed applications from the classic ARP registry keys. Search, sort, and a confirmed uninstall launch.

Host owns chrome and binding. Registry walk, model, and uninstall-string handling live in the Helper. No second façade inside the exe.

---

## 2. Window

| Piece | Rule |
|---|---|
| Shell | `AppMgmtIqWindow` + `VestigiumShell`. |
| Theme | `ThemeManager` in `OnStartup` before the window parses. |
| Pages | Inventory (default). Settings later. |
| Grid | Read-only. Columns from Helper model (DisplayName, Publisher, DisplayVersion, InstallDate, EstimatedSize, UninstallString). |
| Search | Filters the grid. |
| Uninstall | Confirmed launch of the UninstallString. No silent default. |

---

## 3. Behavior

| # | Rule |
|---|---|
| A1 | `HostLog.Initialize(HostIds.AppMgmtIQ)` first, then Themes, then DI, then the window. |
| A2 | MVVM. Code-behind does not call WinReg. |
| A3 | Inventory comes only from Helper. |
| A4 | Uninstall is a confirmed process start of the UninstallString. |
| A5 | No store apps, no winget, no bulk, no repair/modify in v1.0. |

---

## 4. Out of v1.0

Store apps. winget. Bulk uninstall. Repair/Modify. Export. Elevation automation. Per-user vs machine beyond what the registry already supplies.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 16 Oct 2026 | Staging skeleton. Thin host. Logic in Helper. |
