# AppMgmtIQ — Design v1.0

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-SDD-001  
**Version:** 1.0  
**Status:** First release lock  
**Date:** 16 October 2026  
**Project:** `Vestigium.Suite.Windows.AppMgmtIQ`

---

## 1. Boundary

| Layer | Owns |
|---|---|
| Host (`AppMgmtIQ`) | Window, ViewModels, theme, status bar, binding, details window. |
| Helper (`Vestigium.Helpers.WinReg` 1.0.0) | Registry walk of the four ARP roots, model, computed isX64 and isGlobal. |

Host does not re-implement the registry walk.

---

## 2. Startup

1. Logging init with APPID `AppMgmtIQ`.
2. ThemeManager.
3. DI (Themes, Controls, Converters).
4. Window.

---

## 3. Inventory

ViewModel calls Helper. Grid binds to the returned collection. Single selection only. Double-click opens the details window.

---

## 4. Details

Details window title is `DisplayName (DisplayVersion)`. Content is multiple tabs that surface every registry value present for the selected package, plus the computed isX64 and isGlobal fields.

isX64: true when the package is not under a Wow6432Node key.  
isGlobal: true when the package is under HKLM.

---

## 5. Out of this release

Uninstall launch. Search is present but not required for the first lock. Settings page. Anything beyond the four registry roots.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 16 Oct 2026 | Staging. Thin shell. |
| 1.0 | 16 Oct 2026 | First release lock: grid of all packages, single selection, double-click details with all registry values in tabs, computed isX64/isGlobal. |
