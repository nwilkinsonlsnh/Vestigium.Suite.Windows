# PR05 — Requirements

**Document ID:** VEST-SUITE-WINDOWS-APPMGMTIQ-PR05-REQ
**Host:** `Vestigium.Suite.Windows.AppMgmtIQ`
**Status:** Draft
**Date:** 20 October 2026
**Product papers:** [Requirements_v1.0.md](../Requirements_v1.0.md), [Design_v1.0.md](../Design_v1.0.md)

This file is the **definition of done for PR05**. It moves domain work out of the host and into published Helpers. The host becomes a skeleton: it binds, confirms, and shows. The libraries read, resolve, decide, and plan.

**One sentence:** Extract path meaning, startup logic, the bloatware catalog, and machine-state reads into published Helpers. Uninstall decisions stay in the host.

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
| Counters | `{#}` `{##}` `{A}` `{AAA}` `{A#}` `{AA##}` `{a}` `{aaa}` and any width |

`{datetime}`, `{date}`, and `{time}` use a sortable form (`yyyyMMdd-HHmmss`, `yyyyMMdd`, `HHmmss`). Special folders come from `Environment.SpecialFolder` or the matching environment variable. Identity comes from the environment.

**Counters.** A counter token is a run of `#` with an optional letter prefix. The caller passes the current value. The token formats it. It does not roll over.

- `{#}` is one digit, `0` to `9`. `{##}` is two, `00` to `99`. The width is however many `#` the caller writes. Zero-padded.
- A value that does not fit the width throws. `{##}` at `100` throws. It does not become `00`.
- A letter run is a fixed-width base-26 counter of that case, as deep as the caller writes it. `{A}` is one letter. `{AAA}` is three. It counts on the right: `AAA`, `AAB`, `AAZ`, `ABA`, `AZZ`, `BAA`, and on to `ZZZ`. `{a}` and `{aaa}` are the lower-case forms.
- A letter run may be followed by a `#` run. `{A#}` is one capital plus one digit. `{AAA##}` is three capitals plus two digits. The two parts count separately. Letters do not roll into the digits, and the digits do not roll into the letters.
- A letter run throws when it would pass the last value. `{A}` at `Z` throws on the next. `{AA}` at `ZZ` throws on the next. No rollover to a wider run.
- The caller owns the sequence. The token only formats and rejects.

**Long paths.** A conservative plan. When a built path would exceed the classic 260 limit, prefix it with `\\\\?\\` so the call succeeds. Do not prefix a path that is already long-form. Do not change a path that fits.

**Valid path.** `PathCheck.IsValid` answers whether a template is a legal path, not whether the file exists.

- Any text between `{` and `}` is a token and must be known. An unknown token makes the template invalid.
- `{{` is a literal `{`. `}}` is a literal `}`. The doubled brace is not a token.
- A counter that would overflow is invalid.
- After expansion the path must contain no character Windows rejects in a path or a file name.
- An unbalanced brace is invalid.

The host calls these. It does not own the rules.

### R05-02 Uninstall decisions stay in the host

Install health, repair, and uninstall take `PackageRow`, which is a view model. They are rules for this screen, not a library.

- Install health (today named `OrphanRules`). Given an uninstall row, is the install location missing, or is the file named in `DisplayIcon`, `UninstallString`, or `ModifyPath` missing? Stays in the host.
- `RepairPlan`. Builds `msiexec /fa {GUID}` when the row is a Windows Installer product. Stays in the host.
- `UninstallPlan`. Splits the uninstall string and rewrites the quiet flags. Stays in the host.
- Inventory walks and the footprint link stay in the host. They produce host rows.

`BaselineCompare` is the exception. It takes two version strings and returns match, newer, older, unknown, or none. It moves to `Vestigium.Helpers.SystemInfo` as `VersionCompare`. The baseline store stays in the host.

### R05-03 Startup domain

Create `Vestigium.Helpers.Startup` and publish it.

- Approved-state read.
- Launch classification.
- Package link.
- Navigation target.

The elevated projects (`AppMgmtIQ.Startup`, `.Features`, `.Bloatware`, `.ActiveSetup`) stay with the application. They are separate projects only so the host elevates when it must. No helper is created for them. Approved, launch, link, and navigation stay in the host.

### R05-04 Bloatware catalog

`BloatwareCatalog` stays with the application. It feeds the elevated bloatware project. No helper is created.

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
2. Install health, repair, uninstall, the inventory walks, and the footprint link remain in the host. `BaselineCompare` is read from `Vestigium.Helpers.SystemInfo`.
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
| Uninstall rules stay in the host | They take `PackageRow`. A helper named after the host would only serve this host. |
| Baseline compare to SystemInfo | It takes two strings. It is not a package rule. |
| New Startup | Applier stays elevated in the host. |
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
| `Vestigium.Helpers.Installer` | Build it when a second host needs to build an msiexec command from a plain uninstall entry. |

---

## Parked

- Features and DISM helper
- `Vestigium.Helpers.Installer`, with a plain `UninstallEntry` and install-health, repair, and uninstall plans
- Moving the inventory walks
- Moving the export column lists
- Moving settings and MRU
