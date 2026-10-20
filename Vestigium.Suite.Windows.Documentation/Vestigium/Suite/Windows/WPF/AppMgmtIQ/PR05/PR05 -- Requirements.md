# PR05 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PR05-REQ
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`
**Status:** Draft
**Date:** 20 October 2026
**Product papers:** [Requirements_v1.0.md](../Requirements_v1.0.md), [Design_v1.0.md](../Design_v1.0.md)

This file is the **definition of done for PR05**. It moves domain work out of the host and into published Helpers. The host becomes a skeleton: it binds, confirms, and shows. The libraries read, resolve, decide, and plan.

**One sentence:** Extract path resolution, package rules, inventory walks, startup logic, the bloatware catalog, and pending-reboot detection into published Helpers so AppMgmtIQ holds only the UI and the calls.

**This version is not** a behavior change. Not a new tab. Not a new action. The grid, the Exists column, the menus, and the export come back the same.

---

## Must change

### R05-01 FileSystemPaths

Create `Vestigium.Helpers.FileSystemPaths` and publish it. It is a path meaning library: resolve, sanitize, tokenize, and keep long paths safe.

**Resolve.**

- `PathResolve`: strip quotes, drop a `\\??\\` prefix, drop a trailing comma index, expand environment variables.
- `AppPathResolve`: for an empty App Path default, read the `Path` value and join it to the key name; if still empty, search System32, SysWOW64, and PATH.
- Wow64 retry: a `System32` path is retried under `SysWOW64`.
- WinSxS name search for a side-by-side copy.

**Sanitize.**

- `PathSanitize`: make a string safe as a file or folder name. Replace characters Windows rejects (`<>:"/\\|?*` and control characters) with a single safe character. Trim trailing dots and spaces. Collapse repeats. Never return empty.

**Tokens.** A small set, in categories, for building unique file and folder paths. Unknown tokens are left as written.

| Category | Tokens |
|---|---|
| Date and time | `{datetime}` `{date}` `{time}` |
| Special folders | `{desktop}` `{favorites}` `{userprofile}` `{appdata}` `{localappdata}` `{temp}` `{programfiles}` `{programfilesx86}` `{programdata}` `{windir}` `{systemdrive}` `{public}` |
| Identity | `{username}` `{computername}` |
| Counters | `{#}` `{##}` `{A#}` `{AA#}` `{a#}` `{aa#}` and any width |

`{datetime}`, `{date}`, and `{time}` use a sortable form (`yyyyMMdd-HHmmss`, `yyyyMMdd`, `HHmmss`). Special folders come from `Environment.SpecialFolder` or the matching environment variable. Identity comes from the environment.

**Counters.** A counter token is a run of `#` with an optional letter prefix. The caller passes the current value. The token formats it. It does not roll over.

- `{#}` is one digit, `0` to `9`. `{##}` is two, `00` to `99`. The width is however many `#` the caller writes. Zero-padded.
- A value that does not fit the width throws. `{##}` at `100` throws. It does not become `00`.
- A letter prefix is a fixed-width letter counter of that case. `{A#}` is one capital letter plus the digits. `{AA##}` is two capitals plus two digits. `{a#}` and `{aa#}` are the lower-case forms. Letters run `A` to `Z` (or `a` to `z`) and throw past `Z`. They do not roll into the digits, and the digits do not roll into the letters.
- The caller owns the sequence. The token only formats and rejects.

**Long paths.** A conservative plan. When a built path would exceed the classic 260 limit, prefix it with `\\\\?\\` so the call succeeds. Do not prefix a path that is already long-form. Do not change a path that fits.

The host calls these. It does not own the rules.

### R05-02 AppMgmt domain

Create `Vestigium.Helpers.AppMgmt` and publish it.

- Orphan rules. A package is orphaned when the install location or key file is missing.
- Repair and uninstall plans. Build the command. Do not launch it.
- Baseline compare. match / newer / older / unknown / none. The store stays in the host.
- Inventory walks. ARP, App Paths, SharedDLLs, Installer products, associations.
- Footprint to package link.

The host projects rows and binds them.

### R05-03 Startup domain

Create `Vestigium.Helpers.Startup` and publish it.

- Approved-state read.
- Launch classification.
- Package link.
- Navigation target.

The host shows the grid and calls the elevated applier. The applier stays in the host.

### R05-04 Bloatware catalog

Move `BloatwareCatalog` to `Vestigium.Helpers.Bloatware` and publish it. The host reads the catalog. It does not own the list.

### R05-05 Pending reboot

Move the pending-reboot and pending-file-rename read to `Vestigium.Helpers.SystemInfo`. It is machine state, not package state.

---

## Must not change in PR05

- Grid columns, Exists answers, menu actions, and export output
- Confirmation dialogs and elevated appliers
- Settings, MRU, and theme
- Features and DISM (parked)
- Automatic cleanup of any footprint

---

## Acceptance

1. `Vestigium.Helpers.FileSystemPaths` is published and the host no longer owns path rules. It resolves, sanitizes, expands the token set, and applies the long-path prefix only when a path would exceed 260.
2. `Vestigium.Helpers.AppMgmt` is published and the host no longer owns orphan, repair, uninstall, baseline compare, or the inventory walks.
3. `Vestigium.Helpers.Startup` is published and the host no longer owns approved, launch, link, or navigation logic.
4. `Vestigium.Helpers.Bloatware` is published and owns the catalog.
5. Pending reboot is read from `Vestigium.Helpers.SystemInfo`.
6. An export of the same machine matches the PR04 export for Footprints Exists, package counts, and startup rows.
7. No new tab, action, or cleanup.

---

## Decision

| Call | Why |
|---|---|
| Host is a skeleton | Rules belong in libraries so other hosts can use them. |
| New FileSystemPaths | FileIo is file jobs. This is path meaning. |
| New AppMgmt | Package domain is smeared across the host today. |
| New Startup | Same shape as AppMgmt. Applier stays elevated in the host. |
| New Bloatware | The catalog has no UI. |
| Pending reboot to SystemInfo | It is machine state. |
| No behavior change | The test is a matching export. |

### Rejected

| Idea | Why out |
|---|---|
| Put path rules in WinReg | WinReg reads keys. It does not resolve files. |
| Put path rules in FileIo | FileIo runs file jobs. |
| Move elevated appliers | They are host processes with a manifest. |
| Move Features and DISM | Different domain. Bigger move. Parked. |

---

## Parked

- Features and DISM helper
- Moving the export column lists
- Moving settings and MRU
