# PR04a — Requirements

**Document ID:** VEST-SUITE-WINDOWS-FXPLORERIQ-PR04A-REQ
**Host:** `Vestigium.Suite.Windows.FXplorerIQ`
**Status:** Live
**Date:** 22 October 2026

**One sentence:** A Copy, Move, or Mirror does not start until the operator sees the source and picks the destination, and the job window shows the work in the current theme.

**This version is not** a new verb. The verbs stay as PR04 left them.

---

## Must

### R04a-01 No silent destination

Copy, Move, and Mirror never start from the other pane alone. The other pane may prefill the destination. It does not start the job.

A confirm sheet opens first. It shows the source, read only. It shows the destination, editable. Browse opens the folder picker. OK starts the job. Cancel does nothing.

Delete and Delete (Secure) have no destination. They keep the existing confirm.

Audit uses the same sheet. It still changes no disk.

### R04a-02 Folder picker

Browse is a themed WPF window. It is not the Win32 folder dialog.

- This PC at the top. Drives under it. Folders expand on demand.
- The current destination is selected when the picker opens.
- OK returns the selected folder. Cancel returns nothing.
- The picker uses Vestigium.Themes. It matches the host.

### R04a-03 Job window

The job window is themed. Every control uses Vestigium.Themes. No system chrome left on the body.

- Source and destination stay visible for the life of the job. Delete shows the source only.
- Phase, overall bar, rate, and ETA stay.
- The five bins stay. They fill and close out as they do now.
- Pause, resume, and cancel stay. A log miss does not paint as a job failure.

### R04a-04 Live files

The window has a grid of the files in flight.

- Name. Bucket. Size. A per-file bar. Status.
- A file is on the grid while it is queued or active.
- A finished file leaves the grid. A failed file stays and shows the reason.
- The grid scrolls. It does not hold the whole recon.

FileIo does not expose the live list today. That surface is added in the library, published, and then the host is updated. The host does not invent the list.

### R04a-05 Catalogs

The host registers the FileIo catalog and the Analytics catalog at startup, next to its own. Pause, resume, and the FileIo job lines stop throwing `EVENTID is not in the catalog`. A catalog miss is never shown as the job status.

---

## Not in this PR

A new verb. A queue. A jobs history. Changing collision, verify, or shred. Elevating the host.
