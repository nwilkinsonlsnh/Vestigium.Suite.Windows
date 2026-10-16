# AppMgmtIQ — Design v1.0

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-SDD-001  
**Version:** 1.0  
**Status:** Draft — staging  
**Date:** 16 October 2026  
**Project:** `Vestigium.Suite.Windows.AppMgmtIQ`

---

## 1. Boundary

| Layer | Owns |
|---|---|
| Host (`AppMgmtIQ`) | Window, ViewModels, theme, status bar, binding, confirmed uninstall launch. |
| Helper (`Vestigium.Helpers.WinReg` or successor) | Registry walk of ARP keys, model, UninstallString selection. |

Host does not re-implement the registry walk.

---

## 2. Startup

1. Logging init with APPID `AppMgmtIQ`.
2. ThemeManager.
3. DI (Themes, Controls, Converters).
4. Window.

---

## 3. Inventory

ViewModel calls Helper. Grid binds to the returned collection. Search filters in the ViewModel.

---

## 4. Uninstall

User selects a row. Host shows confirmation. On confirm, host starts the process from the UninstallString supplied by the Helper. No silent path in v1.0.

---

## Document control

| Version | Date | Change |
|---|---|---|
| 1.0 | 16 Oct 2026 | Staging. Thin shell. |
