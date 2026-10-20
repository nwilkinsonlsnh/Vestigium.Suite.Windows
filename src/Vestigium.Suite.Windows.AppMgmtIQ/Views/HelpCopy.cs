namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

internal static class HelpCopy
{
    public static IReadOnlyList<Draft> Topics(string version, IReadOnlyList<string> license)
        =>
        [
            Overview(),
            Packages(),
            Features(),
            Bloatware(),
            ActiveSetup(),
            Startup(),
            Footprints(),
            Dashboard(),
            Exports(),
            Settings(),
            new("License", license.Count == 0 ? ["The license file was empty."] : license),
            Glossary(),
            About(version)
        ];

    private static Draft Overview()
        => new("Overview",
        [
            "AppMgmtIQ is a modern, unified management suite designed to give administrators and IT professionals complete visibility into a Windows environment. Traditionally, Windows fragments its ecosystem across multiple distinct interfaces: Add or Remove Programs for the registry, Turn Windows features on or off for the component store, PowerShell for AppX packages, and the registry for Active Setup.",
            "AppMgmtIQ consolidates these inventories into a single, cohesive application. It uses a standardized query bar, consistent data grids, and a unified method for executing changes, so the same habits apply on every tab. Reading is performed locally and does not require elevation. A modification, such as a feature change or a package removal, is handed to a purpose-built elevated worker. The worker performs the task, writes a result, and exits. The suite shows that result and does not stay elevated.",
            Head("AppMgmtIQ"),
            "This is a modernization of Add or Remove Programs. It reads the Windows uninstall registry, which is the list an installer writes when it wants to appear in that classic dialog.",
            "Windows keeps four of these trees. Two are for the machine, under HKEY_LOCAL_MACHINE: one for native 64-bit software and one for 32-bit software under WOW6432Node. Two are for the current user, under HKEY_CURRENT_USER, with the same split. A key in the machine tree is global and applies to every user. A key in the user tree belongs to that profile. A key under WOW6432Node is 32-bit. The others are 64-bit.",
            "The grid is one row per key that has something to show. Display name, version, publisher, install date, and estimated size are the values an administrator looks for first. Double-click a row and every value on that key is shown, grouped, rather than dumped as a raw export. The window title is the display name and the version, and it follows the selection.",
            "Uninstall uses the command the installer left behind. Product runs that UninstallString as it was stored, with a path taken through the executable so a folder such as Program Files is not split. Forced uninstall is offered only for a Windows Installer package, and it builds msiexec /x with a quiet or basic interface instead of trusting the stored command. A package marked NoRemove can have that flag cleared, and the write is logged with the account that did it before the uninstall proceeds.",
            Head("Windows Features"),
            "Optional features are the components Windows can add or remove without a separate installer. Internet Information Services, Hyper-V, and the legacy components all live in the component store. The built-in dialog is Turn Windows features on or off. This tab reads the same store and presents the same tree.",
            "A feature has a state. Enabled means it is on. Disabled means it is installed and off. Absent means it is not on the machine. A feature also has a parent, and the tree is that relationship. A group such as WCF Services contains children, and the parent is a summary of them.",
            "The checkbox on a parent is tri-state for that reason. Checked means the feature and everything under it are on. The middle state means some children are on. Clear means none are. A leaf is only ever checked or clear. Checking a child that requires its parent selects the parent too, so the summary that follows is not a surprise.",
            "Nothing is written while you check and clear. Apply Feature Changes opens an elevated worker, centered on this window, and lists exactly what will be enabled and what will be disabled. On continue it applies those states and suppresses a reboot where Windows allows it. The summary lists each feature and any error.",
            Head("Bloatware"),
            "Inbox applications and Microsoft Store packages are AppX packages. They are not in the uninstall registry. They are registered with the AppX deployment service, and a provisioned copy can be kept on the machine so that every new user profile receives the package at first logon.",
            "This tab starts from a catalog, not from an inventory. The catalog is a known list of inbox and Store packages: the Bing apps, the Xbox components, the games that ship on a new image, Teams, and the packages Microsoft has since retired. Every catalog entry is shown. Installed and Provisioned say whether this machine has it. A row that is not installed is a package we know about, not a package that is here.",
            "Installed means the package is registered for this user and can be launched. Provisioned means the machine holds a copy for new users. Removing only the user registration leaves the provisioned copy, and the next person who logs on receives it again. A real removal takes both.",
            "Remove is elevated. It removes the package for every user by its full package name, then removes the provisioned copy when the row is provisioned. Add looks for an AppxManifest.xml already on the disk, from a provisioned package or from another user's copy, and registers that. If there is no local manifest, it asks winget to fetch the package, preferring the Store match that is not a classic or a plugin. If the package is already installed, that is a success. If Microsoft has retired it, the window says so.",
            Head("Startup"),
            "Startup is the set of things registered to run when the user logs on. It is the Run and RunOnce keys, RunOnceEx, and the two Startup folders. Disable is a Windows flag next to the entry, not a deletion, so a disabled item stays in the grid.",
            Head("Footprints"),
            "Footprints is the installer side effects. App Paths, SharedDLLs, Windows Installer product codes, and installer-linked associations. It is read-only. After the grid is shown, a background pass checks whether each file path still exists.",
            Head("Active Setup"),
            "Active Setup is a logon mechanism, and it is rarely audited. It is not an installer and it is not a scheduled task. At logon, Windows compares a machine key with a per-user key. If the machine version is higher, or the user has no key, Windows runs the command in StubPath and then writes the version into the user's key so it does not run again.",
            "The keys live under SOFTWARE\\Microsoft\\Active Setup\\Installed Components, in HKEY_LOCAL_MACHINE and HKEY_CURRENT_USER, and again under WOW6432Node for 32-bit. The key name is the component, usually a GUID. The values that matter are the display name, StubPath, Version, and IsInstalled. Version is compared as comma-separated numbers, not as text.",
            "Pending means the machine copy is ahead of this user's copy, so the command will run at the next logon. A component with no StubPath has nothing to run. That is normal for a component that only records a version.",
            "A StubPath is an arbitrary command. Replacing one, or adding a new component, is a way to run code for every user at logon. That is why the tab exists. Add, Delete, and Clear command are on the menu. A user-key change is written directly. A machine-key change is handed to an elevated worker. Delete removes the key. Clear command removes StubPath and leaves the version, so the component stays but will not run.",
            Head("Dashboard and Exports"),
            "The dashboard is a reading of the inventories, not another one. Each section is one inventory, with the counts that describe it and the charts that break it down.",
            "AppMgmtIQ shows who published the software, how many entries are orphaned, which packages can be repaired or modified, and how the installed version compares with a baseline. Windows Features shows how many are on and which parents own them. Bloatware shows how much of the catalog is actually present. Active Setup shows how much is armed to run, and whether it is a machine or a user component. Startup shows how many entries are on, how many are off, and how many are linked to a package. Footprints shows App Paths, shared DLLs, installer products, and how many of the file paths still exist. A section with two charts shows them together. A section with more than two gets a vertical list, and the area shows the one you pick.",
            "Export writes a workbook from those same inventories. The first sheet is a summary and is the sheet Excel opens on. Further sheets are added for the inventories you include: the package grid, the package details, and the feature tree. Current filters exports what the grid is showing. All exports the inventory regardless of the query."
        ]);

    private static Draft Packages()
        => new("AppMgmtIQ",
        [
            "This primary module functions as a comprehensive reader for the Windows uninstall registry, the same underlying data used by Add or Remove Programs since Windows 2000. Whenever an installer registers a product, it writes a key, typically a product code or a name, containing descriptive values such as DisplayName, DisplayVersion, Publisher, InstallDate, EstimatedSize, and UninstallString, alongside operational flags like NoRemove, NoModify, SystemComponent, and WindowsInstaller.",
            Head("Registry Architecture and Scope"),
            "Windows maintains four distinct uninstall trees, all of which are seamlessly consolidated into this single view.",
            Term("Machine-Wide (Global)", "Stored under HKEY_LOCAL_MACHINE. Software native to 64-bit Windows resides here, while 32-bit (x86) software is routed to the WOW6432Node. Packages in this tree are flagged as Global in the grid."),
            Term("Per-User", "Stored under HKEY_CURRENT_USER with the same 64-bit and 32-bit split. These packages belong strictly to the active user profile."),
            Term("Architecture", "Packages located under the WOW6432Node are identified as 32-bit, while the others are marked via the x64 column."),
            Head("Interface and Details"),
            "The main interface presents one row per valid registry key, displaying core attributes and checkbox flags at a glance. Double-clicking any row opens a detailed, organized view of every registry value associated with that key, intelligently grouped rather than dumped as raw text. The detail window dynamically updates its title to reflect the selected application's display name and version.",
            Head("Uninstall Mechanics"),
            "When an uninstallation is triggered, the application processes the action based on the installer's properties.",
            Term("Command Execution", "The tool relies on the stored UninstallString. It safely handles paths with spaces by evaluating them through the .exe boundary, ensuring paths like C:\\Program Files are not erroneously split into C:\\Program."),
            Term("Forced Uninstall", "This option is exclusively available for Windows Installer packages. Rather than trusting the stored command string, it explicitly builds an msiexec /x command utilizing a basic or quiet interface."),
            Term("Overriding Restrictions", "If a package has secured itself using the NoRemove flag, the module can override it. Clearing this restriction writes a 0 to the registry value, an action that is securely logged under the modifying account before the uninstall is permitted to proceed."),
            Head("Search and Filtering"),
            "The query bar acts as a live filter for the data grid. Entering a generic word searches every column simultaneously. For precision filtering, operators can query specific named fields: app.displayname, app.publisher, app.version, app.InstallDate, and app.size.mb."
        ]);

    private static Draft Features()
        => new("Windows Features",
        [
            "The Windows Features module provides direct management of the Windows component store, allowing administrators to add or remove optional system components without requiring separate external installers. This interface serves as a powerful, modern replacement for the legacy Turn Windows features on or off dialog, handling native elements such as Internet Information Services (IIS), Hyper-V tools, and various legacy components.",
            Head("Feature States and Hierarchy"),
            "Every feature within the component store exists in one of three specific states.",
            Term("Enabled", "The feature is actively turned on."),
            Term("Disabled", "The feature is installed on the system but currently turned off."),
            Term("Absent", "The feature's payload is entirely removed from the machine."),
            "Features are organized into a strict parent-child hierarchical tree. Because a parent node acts as a structural summary of the features beneath it, its checkbox operates in a tri-state mode. A fully checked box indicates that the parent and all of its children are enabled. A clear box indicates none are enabled. An indeterminate, or middle, state signifies that only some children are enabled. Leaf nodes, features without children, only utilize a standard checked or clear state. Checking a dependent child automatically selects its required parent, ensuring there are no hidden dependencies when it is time to apply the configuration.",
            Head("Interface and Navigation"),
            "The main grid cleanly exposes the underlying architecture of each component by displaying its Display Name, Feature Name, Parent, and current State. For easier navigation through complex trees like .NET Framework or IIS services, users can utilize Expand, Collapse, Expand All, and Collapse All functions directly from the menu. To maintain a consistent workflow, the expanded or collapsed state of the entire tree is persistently remembered in the application's Settings.",
            Head("Applying Feature Changes"),
            "Unlike direct registry edits, checking or clearing boxes in this tab simply stages the intended configuration locally. No changes are written to the system immediately.",
            Term("Apply Feature Changes", "Invokes a dedicated, elevated worker process centered on the screen."),
            Term("Confirmation", "The worker acts as a final confirmation, explicitly listing exactly which features are staged to be enabled and which will be disabled."),
            Term("Execution", "The worker processes the queue and suppresses system reboots wherever Windows allows it."),
            Term("Summary", "Once finished, it presents a summary listing each targeted feature along with any associated errors, and the main suite automatically refreshes its inventory to reflect the new state.")
        ]);

    private static Draft Bloatware()
        => new("Bloatware",
        [
            "This module specializes in managing AppX packages, commonly referred to as inbox applications or bloatware. Because these applications do not reside in the standard Windows uninstall registry, they must be managed through the AppX deployment service.",
            Head("A Catalog-First Approach"),
            "Unlike the other inventories that strictly read what is present on the machine, the Bloatware tab operates from a predefined catalog of known inbox and Microsoft Store packages. This extensive list includes Bing applications, Xbox components, pre-installed games such as Candy Crush, modern utilities like Dev Home and Copilot, and even legacy applications that Microsoft has since retired. Because every catalog entry is shown, a row indicating that it is not installed simply means it is a tracked package missing from the current machine.",
            Head("Package States: Installed vs. Provisioned"),
            "The grid clearly categorizes the status of each catalog item using two critical metrics.",
            Term("Installed", "The package is actively registered to the current user profile and can be launched immediately."),
            Term("Provisioned", "A master copy of the package is stored on the machine's disk. When a new user logs into the machine for the first time, Windows automatically uses this provisioned copy to deploy the application to their profile."),
            "A true removal requires addressing both states. Removing only the user registration leaves the provisioned copy intact, guaranteeing the application will return the next time a new profile is created.",
            Head("Intelligent Add and Remove Workflows"),
            "Interacting with packages is designed to be comprehensive and system-aware. Right-clicking a row dynamically presents context options. Add is offered for missing packages, while Remove is available for installed ones.",
            Term("Elevated Removal", "The removal process runs elevated to ensure total eradication. It uninstalls the target package across every user profile by its full package name. Crucially, if the interface indicates the package is provisioned, the worker also deletes the provisioned master copy, ensuring the bloatware cannot silently reinstall for future users."),
            Term("Intelligent Add and Restore", "When adding a package, the tool prioritizes local resources. It first scans the disk for an existing AppxManifest.xml, either from an existing provisioned copy or another user's profile, and securely registers it. If a local manifest cannot be found, the system falls back to winget to download the package. If winget returns multiple options, the tool automatically filters for the precise Store match, explicitly ignoring classic desktop installers and plugins. Furthermore, if a package has been officially retired by Microsoft, the application transparently reports this status instead of attempting to fetch a non-existent download.")
        ]);

    private static Draft ActiveSetup()
        => new("Active Setup",
        [
            "Active Setup is a specialized Windows logon mechanism, distinct from standard installers or scheduled tasks, that is frequently overlooked and rarely audited. Because it can execute arbitrary commands, the StubPath, for every user at logon, it presents a significant surface area for both routine system management and potential exploitation. This tab brings complete visibility to these hidden components, allowing administrators to see exactly what is configured to run when a profile initializes.",
            Head("Architecture and Execution Mechanics"),
            "Windows processes Active Setup by comparing a machine-level registry key, HKEY_LOCAL_MACHINE, against a corresponding per-user key, HKEY_CURRENT_USER, during the logon sequence. The keys reside under SOFTWARE\\Microsoft\\Active Setup\\Installed Components, with 32-bit components correctly mapped to the WOW6432Node.",
            "If the machine's version number is higher than the user's, or if the user lacks the key entirely, Windows executes the command defined in the component's StubPath. Once the execution completes, Windows stamps the updated version into the user's registry hive to prevent the command from running again. Crucially, the system compares these versions as comma-separated numbers, not as standard text strings.",
            Head("Interface and Component States"),
            "The data grid lists each component, typically identified by a GUID, along with its critical values: Display Name, StubPath, Version, and IsInstalled status.",
            Term("Pending", "When a component is marked as Pending, it indicates that the machine copy is ahead of the current user's copy. This serves as a direct alert that the associated command is armed and waiting to run at the very next logon."),
            Term("Dormant Components", "Components that lack a StubPath are completely normal. They simply record version data without requiring execution."),
            Head("Applying System Changes"),
            "Administrators can proactively manage these logon triggers using the Add, Delete, and Clear command options found in the menu.",
            Term("Targeted Writes", "Changes made to a current user's key are written directly. Conversely, modifications to machine-level keys are handed to an elevated worker process that securely applies the change, exits, and instantly returns a summary result without requiring a redundant secondary confirmation window."),
            Term("Remediation Options", "While Delete entirely removes the component key from the registry, the Clear command action is more surgical. It strips only the StubPath while preserving the version data, ensuring the component remains registered but is effectively neutralized from running code."),
            Head("Search and Filtering"),
            "The query bar is tailored specifically for the Active Setup catalog. Administrators can quickly isolate executable components by typing setup.stubpath IS NOT NULL, or filter for dormant keys by querying setup.stubpath IS NULL."
        ]);


    private static Draft Startup()
        => new("Startup",
        [
            "The Startup tab is the persistence view. It lists what is registered to run when a user logs on, links each entry to an installed package when a match exists, and lets an administrator turn those entries on or off. It is not a scheduled-task editor and it is not a service controller.",
            Head("What is read"),
            "The scan covers the locations Windows itself uses for logon.",
            Term("Run", "HKLM and HKCU under CurrentVersion\\Run. HKLM is read in both the 64-bit and 32-bit views."),
            Term("RunOnce", "The same split. These run once and then Windows removes them."),
            Term("RunOnceEx", "The HKLM key and its subkeys, in both views. Used by installers that need an ordered pass."),
            Term("Startup folder", "The per-user Startup folder and the common Startup folder under ProgramData. A shortcut is resolved to its target when that is possible."),
            "A row with no display name uses the value name, or the file name, as the fallback. The Package column is a best-effort match against the inventory, by path or name. A blank package means no confident match.",
            Head("Enabled and disabled"),
            "Windows does not delete a Run value to turn it off. It writes a flag next to it, under Explorer\\StartupApproved, in the Run, Run32, or StartupFolder branch. The first byte of that value is the state. 02 or 06 means Enabled. Anything else means Disabled. A missing flag means Enabled, which is how Task Manager and Settings treat it.",
            "Disable and Enable write that flag. The Run value stays, the row stays in the grid, and the package link stays. That is the same flag Task Manager uses, so other tools agree with the result.",
            "RunOnce and RunOnceEx have no flag. They always show as Enabled, and Enable and Disable skip them. Remove is the only write for those.",
            "This tab does not use AutorunsDisabled. That is a Sysinternals convention. Windows does not read it, and a tool that moves a value there makes the entry look deleted rather than disabled.",
            Head("Changing an entry"),
            "The menu is on the row.",
            Term("Enable and Disable", "Write the StartupApproved flag for the selected row."),
            Term("Enable All and Disable All", "Do the same for every row in the current filtered view, after a confirm that shows the count and the HKCU / HKLM split. A filter of startup.source == 'Run' does not touch Startup folder entries."),
            Term("Remove", "Deletes the registry value or the Startup folder item. It does not uninstall the package. Confirm first."),
            Term("Open source", "Opens the registry key in Regedit, or the folder in Explorer."),
            Term("Jump to package", "Selects the linked package on the inventory tab. Disabled when the row is not linked."),
            "A user write, HKCU Run or the per-user Startup folder, is done in this process. No elevation. A machine write, HKLM or the common Startup folder, is handed to AppMgmtIQ.Startup, an elevated worker, the same pattern as Features and Bloatware. A mixed bulk action does the user writes here and the machine writes in one elevated launch. Cancelling the prompt leaves the user writes as they are and reports the machine writes as cancelled.",
            Head("Search"),
            "The query bar filters the grid against the Startup catalog. A word searches the columns. Named fields are startup.name, startup.command, startup.source, startup.scope, startup.isX64, startup.package, and startup.enabled. startup.enabled == false is the disabled set."
        ]);

    private static Draft Footprints()
        => new("Footprints",
        [
            "The Footprints tab provides a read-only audit of the artifacts and side effects left behind by software installers. It is designed purely as a diagnostic view rather than a registry editor, revealing the exact locations an installer modifies when instructing Windows to locate a program, share a DLL, or remember a product registration.",
            Head("Tracked Artifacts"),
            "The scan targets four distinct categories of installer footprints, which are identified in the Kind column.",
            Term("App Paths", "Registry locations Windows uses to resolve program execution. The scan reads HKLM, the 32-bit WOW6432Node, and HKCU under CurrentVersion\\App Paths. The key name represents the executable, while the default value holds the target path."),
            Term("SharedDLLs", "File paths for shared libraries. The scan checks HKLM and its 32-bit redirected view. The stored registry data represents the active reference count for that specific shared file."),
            Term("Products", "Windows Installer product codes stored under Installer\\UserData. The system unpacks packed registry names into standard GUIDs. Installations found under the S-1-5-18 SID signify a machine-wide context, while any other SID indicates a per-user installation."),
            Term("Associations", "Installer-linked ProgIDs and their mapped file extensions found under the Classes keys in both HKLM and HKCU. A ProgID is included in this grid only if it carries an InstallSource, a ProductCode, or maps to a product code already established in the inventory."),
            Head("Scope Definitions"),
            "The Scope column clarifies the exact registry origin or context of the footprint, rather than relying on a single vocabulary.",
            Term("HKLM", "The native 64-bit Local Machine view, applicable to App Paths, SharedDLLs, and Associations."),
            Term("Wow", "The 32-bit redirected view (WOW6432Node), functioning as Local Machine space for App Paths and SharedDLLs."),
            Term("HKCU", "The current user hive, applicable to App Paths and Associations."),
            Term("Machine", "A Windows Installer product registered under the SYSTEM SID, designating a machine-wide install."),
            Term("User", "A Windows Installer product registered under a specific user SID, designating a per-user install."),
            Head("File Existence Verification"),
            "To prioritize performance, the grid populates instantly as soon as the registry read concludes. Immediately after, a background pass scans the disk to verify if paths that resemble files still physically exist. Product codes and ProgIDs are skipped entirely.",
            Term("Yes", "The system successfully located the file at the specified path."),
            Term("No", "The path resembles a file, but File.Exists returned false, indicating a broken or orphaned footprint."),
            Term("Blank", "The verification check is either still processing, or the targeted path does not represent a standard file."),
            "Once this background pass settles, the bottom status line automatically updates to report the total number of missing files. You can refresh the Dashboard after this pass completes to capture the final validated counts.",
            Head("Package Mapping"),
            "The Package column attempts a best-effort resolution to link the raw footprint back to a known software package in your primary inventory. A successful match is made if a path resides directly within a known installation location, if a product code in the path matches a package's uninstall key, or if a display name of at least four characters matches when no file path is available. A blank entry in this column simply means the system could not confidently establish a match."
        ]);

    private static Draft Dashboard()
        => new("Dashboard",
        [
            "The Dashboard serves as a unified visual reading of the application's inventories. Rather than running new scans, it synthesizes the data already loaded into the suite's memory. Clicking Refresh instantly rebuilds all summary figures and charts to reflect the current state of the machine.",
            "The interface adapts its layout based on the data. When a section features exactly two charts, they are displayed side by side. For sections containing more than two, a vertical selection menu on the left allows the reader to toggle between views. Ranked charts overlay a cumulative line, making the concentration of the inventory visible at a glance.",
            Head("AppMgmtIQ"),
            "This section visualizes the primary software inventory, detailing what is installed, who published it, and its footprint on the disk.",
            Term("Counts", "The total number of packages, alongside how many are global, how many belong strictly to the current user, and the percentage share that are 64-bit."),
            Term("Publishers", "A ranked view of software publishers featuring a cumulative share line."),
            Term("Mix", "The split between global and per-user installations, as well as system components versus 32-bit packages."),
            Term("Install year", "The inventory grouped chronologically by installation date. Packages missing a date are reported as Unknown."),
            Term("Size", "The software inventory distributed by estimated size in megabytes."),
            Head("Windows Features"),
            "This section answers exactly which optional components are active and maps out their parent group ownership.",
            Term("Counts", "The total number of features tracked, broken down into how many are enabled, disabled, and absent."),
            Term("Feature state", "The proportion of enabled features against disabled ones."),
            Term("Feature parents", "A ranked view of which parent groups own the most features, with a cumulative line. Root features, those with no parent, are reported separately."),
            Head("Bloatware"),
            "This section analyzes the curated catalog of inbox and Microsoft Store apps against what is actually present on the local machine.",
            Term("Counts", "The total catalog size alongside the number of packages that are installed, provisioned for new users, or missing."),
            Term("Category", "The catalog across System, Productivity, Xbox, Store, Media, and Games."),
            Term("Presence", "Installed packages against those that are not installed."),
            Term("Provisioned", "How much of the catalog is held as a provisioned master copy."),
            Head("Active Setup"),
            "This section audits the logon mechanisms, providing visibility into what is armed to execute during the next user initialization.",
            Term("Counts", "The total tracked components, how many are pending execution, how many contain a command, and how many are configured at the machine level."),
            Term("Pending", "Components waiting to run against those already current for the user."),
            Term("Scope", "The split between machine-wide triggers and current-user keys."),
            Term("Architecture", "Components categorized by 64-bit and 32-bit architecture."),
            Term("Command", "Components with an executable command against those serving only as a version record."),
            Head("Startup"),
            "This section shows what starts with Windows, which of those are enabled, and which package they belong to.",
            Term("Counts", "Total entries, how many are enabled, how many are disabled, and the share that linked to a package."),
            Term("State", "Enabled against disabled."),
            Term("Source", "Run, RunOnce, RunOnceEx, and the Startup folders."),
            Term("Scope", "HKLM, HKCU, Common, and User."),
            Term("Linked", "Entries that matched a package against those that did not."),
            Head("Footprints"),
            "This section reads the installer side effects already loaded by the Footprints tab.",
            Term("Counts", "Total rows, App Paths, SharedDLLs with a reference count above 1, and installer products."),
            Term("Type", "App Paths, Shared DLLs, Products, and Associations."),
            Term("Exists", "Paths that still exist, paths that do not, and rows with no status yet. No status is a product code, a ProgID, or a file check that has not finished."),
            Term("Ref count", "SharedDLL reference counts in buckets of 1, 2, 3 to 5, and 6 or more."),
            Term("Linked", "Rows that matched a package against those that did not.")
        ]);

    private static Draft Exports()
        => new("Exports",
        [
            "Export writes a single Excel workbook from the inventories already loaded into the suite. It does not run a new scan. Summary is the first sheet, and it is the sheet Excel opens on. A further sheet is added for each inventory that is included. The file is written to Desktop\\Vestigium\\Exports\\AppMgmtIQ.",
            Head("Inventories"),
            "The four inventories are independent, and each is a checkbox. Include any combination. At least one is required. On a new installation all four are checked.",
            Term("AppMgmtIQ", "The package grid. One row per uninstall registry key, with display name, publisher, version, install date, size, architecture, scope, and the operational flags."),
            Term("Windows Features", "The component store. Display name, feature name, state, parent, and description."),
            Term("Bloatware", "The catalog. Name, family, category, whether it is installed, whether it is provisioned, and the package name."),
            Term("Active Setup", "The logon components. Display name, component, StubPath, version, scope, architecture, pending, and installed."),
            Head("Rows"),
            "Current view respects the query bar and the filter menus on each page, so the workbook matches what is on screen. Everything loaded ignores those filters and writes the full inventory for each included section.",
            Head("Package Details"),
            "Include package registry values adds a Package Details sheet. It is the same set of values the details window shows for each package, and it is only available when AppMgmtIQ is included.",
            Head("What is remembered"),
            "The choices on this page are saved with the application settings, under C:\\ProgramData\\Vestigium\\Settings\\AppMgmtIQ. Which inventories are included, Current view or Everything loaded, and whether package details are included all survive a restart."
        ]);

    private static Draft Settings()
        => new("Settings",
        [
            "Settings are the defaults for this app, stored as JSON under C:\\ProgramData\\Vestigium\\Settings\\AppMgmtIQ. They survive a restart. Logs are written next to them, under C:\\ProgramData\\Vestigium\\Logs\\AppMgmtIQ, and a change to a setting is logged with the old value and the new one.",
            "Show global and show per-user decide which uninstall trees are included. The date format is how InstallDate is rendered. Decimal places is how EstimatedSize is rounded when it is shown as KB, MB, or GB. The Windows Features tree remembers whether it was left expanded or collapsed."
        ]);

    private static Draft Glossary()
        => new("Glossary",
            ["The words used on the grids, and what they mean in Windows."],
            [
                new("Packages",
                [
                    Term("Display name", "The name the installer wrote. The details title uses it with the version."),
                    Term("Global", "The key is under HKEY_LOCAL_MACHINE. It applies to every user."),
                    Term("Per user", "The key is under HKEY_CURRENT_USER. It applies to this profile."),
                    Term("x64", "The native uninstall key. x86 is the same key under WOW6432Node."),
                    Term("System component", "SystemComponent is set. Add or Remove Programs hides these."),
                    Term("Windows Installer", "The package was installed by msiexec. Forced uninstall is available."),
                    Term("NoRemove", "The package set this flag. Clearing it is a logged write."),
                    Term("UninstallString", "The command the installer left. Product runs it.")
                ]),
                new("Features",
                [
                    Term("Enabled", "The feature is on."),
                    Term("Disabled", "The feature is installed and off."),
                    Term("Absent", "The feature is not installed."),
                    Term("Parent", "The feature this one sits under. A parent checkbox summarizes its children."),
                    Term("Tri-state", "Checked, partial, or clear. Only a parent can be partial.")
                ]),
                new("Bloatware",
                [
                    Term("Catalog", "The known list. A row is not proof the package is installed."),
                    Term("Installed", "Registered for this user with the AppX service."),
                    Term("Provisioned", "Held for every new user. A new profile receives it at first logon."),
                    Term("Retired", "Microsoft no longer ships or offers the package.")
                ]),
                new("Active Setup",
                [
                    Term("Component", "The key name, usually a GUID."),
                    Term("StubPath", "The command run at logon when the version says it should."),
                    Term("Version", "Comma-separated numbers. A higher machine version runs again."),
                    Term("Pending", "This user does not yet have the machine version."),
                    Term("IsInstalled", "The component is marked ready. It is not proof the command succeeded.")
                ]),
                new("Startup",
                [
                    Term("Run", "A value that runs at every logon."),
                    Term("RunOnce", "A value that runs once. No disable flag."),
                    Term("StartupApproved", "The Windows on/off flag. The Run value stays."),
                    Term("Enabled", "Flag first byte is 02 or 06, or the flag is missing."),
                    Term("Disabled", "Flag is present and is not enabled."),
                    Term("Scope", "HKLM, HKCU, Common, or User."),
                    Term("Package", "Best-effort link to an installed package. Blank when unmatched.")
                ]),
                new("Footprints",
                [
                    Term("App Path", "A program name whose default value is the path Windows should run."),
                    Term("SharedDLL", "A file path with a reference count. More than 1 means more than one product claims it."),
                    Term("Product", "A Windows Installer product code. Machine is S-1-5-18. User is any other SID."),
                    Term("Association", "An installer-linked ProgID or the extension that points at it."),
                    Term("HKLM", "The 64-bit Local Machine view."),
                    Term("Wow", "The 32-bit Local Machine view under WOW6432Node."),
                    Term("Exists", "Yes if the file was found. No if it was not. Blank if the check has not finished or the path is not a file.")
                ])
            ]);

    private static Draft About(string version)
        => new("About",
            ["AppMgmtIQ " + version + "."],
            [
                new("App",
                [
                    "AppMgmtIQ " + version + ". Installed packages, Windows features, catalog apps, and Active Setup, in one suite.",
                    "Copyright (c) 2026 Nathaniel Wilkinson. MIT License. The notice is the License topic.",
                    "The public source for this host is Vestigium.Suite.Windows.",
                    Link("https://github.com/nwilkinsonlsnh/Vestigium.Suite.Windows", "Vestigium.Suite.Windows")
                ])
            ]);

    private static string Head(string line) => "\x1f" + line;
    private static string Link(string url, string label) => "\x1c" + url + "|" + label;
    private static string Term(string word, string means) => "\x1b" + word + "|" + means;
}

internal sealed record Draft(string Title, IReadOnlyList<string> Lines, IReadOnlyList<Draft> Sections)
{
    public Draft(string title, IReadOnlyList<string> lines) : this(title, lines, [])
    {
    }
}

