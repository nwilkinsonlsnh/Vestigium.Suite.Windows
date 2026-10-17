# AppMgmtIQ — Requirements v1.0

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-SRS-001  
**Version:** 1.0  
**Status:** First release lock  
**Date:** 16 October 2026  
**Project:** `Vestigium.Suite.Windows.AppMgmtIQ`  
**Kind:** WPF exe, `net10.0-windows`, MVVM  
**APPID:** `AppMgmtIQ`  
**Logic:** `Vestigium.Helpers.WinReg` (and any future AppMgmt helper)  
**Chrome:** `Vestigium.Themes`, `Vestigium.Controls`, StatusBar, NumericUpDown, UnderConstruction, Converters  
**Logging:** `Vestigium.Logging`

---

## 1. What ships

One thin host window. Main view is a grid of all installed packages from the classic ARP registry keys. Single selection only. Double-click a row opens an organized details window that shows every registry value for that package across tabs, plus computed fields.

Host owns chrome, binding, and the details window. Registry walk, model, and computed fields live in the Helper. No second façade inside the exe.

---

## 2. Inventory sources

The Helper enumerates these keys (and their subkeys):

- `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\`
- `HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall`
- `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall`
- `HKEY_CURRENT_USER\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall`

Only subkeys that represent an installed package are returned. Packages with no DisplayName are still listed (key name is used as fallback display).

---

## 3. Window

| Piece | Rule |
|---|---|
| Shell | `AppMgmtIqWindow` + `VestigiumShell`. |
| Theme | `ThemeManager` in `OnStartup` before the window parses. |
| Main view | Grid of all packages. |
| Selection | Single selection only. |
| Grid columns | DisplayName, Publisher, DisplayVersion, InstallDate, EstimatedSize, isX64, isGlobal, UninstallString (and any other high-value fields the Helper surfaces). |
| Details | Double-click a row opens the details window. |
| Details title | `DisplayName (DisplayVersion)`. Updates when the selected package changes. |
| Details content | Multiple tabs that organize every registry value present for that package. |
| Computed | `isX64` (bool): true = x64, false = x86. `isGlobal` (bool): true = HKLM, false = HKCU. |

---

## 4. Behavior

| # | Rule |
|---|---|
| A1 | `HostLog.Initialize(HostIds.AppMgmtIQ)` first, then Themes, then DI, then the window. |
| A2 | MVVM. Code-behind does not call WinReg. |
| A3 | Inventory comes only from Helper. |
| A4 | Single selection only. |
| A5 | Double-click opens details. Title is DisplayName (DisplayVersion). |
| A6 | Details window shows all registry values for the selected package, organized across tabs. |
| A7 | isX64 and isGlobal are computed by the Helper and shown in both grid and details. |
| A8 | No store apps, no winget, no bulk, no repair/modify, no uninstall launch in this first release. |

---

## 5. Out of this release

Store apps. winget. Bulk operations. Repair/Modify. Uninstall launch. Export. Elevation automation. Anything beyond the four registry roots above.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 16 Oct 2026 | Staging skeleton. Thin host. Logic in Helper. |
| 1.0 | 16 Oct 2026 | First release lock: grid of all packages from the four ARP keys, single selection, double-click details window with all registry values organized in tabs, computed isX64 and isGlobal, title = DisplayName (DisplayVersion). |
