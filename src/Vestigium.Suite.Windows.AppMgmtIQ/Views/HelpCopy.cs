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
            "AppMgmtIQ consolidates these four critical inventories into a single, cohesive application. It uses a standardized query bar, consistent data grids, and a unified method for executing changes, so the same habits apply on every tab. Reading is performed locally and does not require elevation. A modification, such as a feature change or a package removal, is handed to a purpose-built elevated worker. The worker performs the task, writes a result, and exits. The suite shows that result and does not stay elevated.",
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
            Head("Active Setup"),
            "Active Setup is a logon mechanism, and it is rarely audited. It is not an installer and it is not a scheduled task. At logon, Windows compares a machine key with a per-user key. If the machine version is higher, or the user has no key, Windows runs the command in StubPath and then writes the version into the user's key so it does not run again.",
            "The keys live under SOFTWARE\\Microsoft\\Active Setup\\Installed Components, in HKEY_LOCAL_MACHINE and HKEY_CURRENT_USER, and again under WOW6432Node for 32-bit. The key name is the component, usually a GUID. The values that matter are the display name, StubPath, Version, and IsInstalled. Version is compared as comma-separated numbers, not as text.",
            "Pending means the machine copy is ahead of this user's copy, so the command will run at the next logon. A component with no StubPath has nothing to run. That is normal for a component that only records a version.",
            "A StubPath is an arbitrary command. Replacing one, or adding a new component, is a way to run code for every user at logon. That is why the tab exists. Add, Delete, and Clear command are on the menu. A user-key change is written directly. A machine-key change is handed to an elevated worker. Delete removes the key. Clear command removes StubPath and leaves the version, so the component stays but will not run.",
            Head("Dashboard and Exports"),
            "The dashboard is a reading of the four inventories, not a fifth one. Each section is one inventory, with the counts that describe it and the charts that break it down.",
            "AppMgmtIQ shows who published the software, how it splits between machine and user, when it was installed, and how large it is. Windows Features shows how many are on and which parents own them. Bloatware shows how much of the catalog is actually present. Active Setup shows how much is armed to run, and whether it is a machine or a user component. A section with two charts shows them together. A section with more than two gets a vertical list, and the area shows the one you pick.",
            "Export writes a workbook from those same inventories. The first sheet is a summary and is the sheet Excel opens on. Further sheets are added for the inventories you include: the package grid, the package details, and the feature tree. Current filters exports what the grid is showing. All exports the inventory regardless of the query."
        ]);

    private static Draft Packages()
        => new("AppMgmtIQ",
        [
            "This primary module is a comprehensive reader for the Windows uninstall registry, the same underlying data Add or Remove Programs has used since Windows 2000. Whenever an installer registers a product, it writes a key, typically a product code or a name, containing descriptive values such as DisplayName, DisplayVersion, Publisher, InstallDate, EstimatedSize, and UninstallString, alongside operational flags such as NoRemove, NoModify, SystemComponent, and WindowsInstaller.",
            Head("Registry architecture and scope"),
            "Windows maintains four distinct uninstall trees. This view consolidates all of them.",
            "Machine-wide, or global, is stored under HKEY_LOCAL_MACHINE. Software native to 64-bit Windows resides in the main key, while 32-bit software is routed to WOW6432Node. Packages in this tree are flagged as Global in the grid.",
            "Per-user is stored under HKEY_CURRENT_USER, with the same 64-bit and 32-bit split. These packages belong strictly to the active user profile.",
            "Architecture follows the key. A package under WOW6432Node is 32-bit. The others are marked x64.",
            Head("Interface and details"),
            "The main interface presents one row per valid registry key, with the core attributes and the checkbox flags at a glance. Double-click a row and every registry value on that key is shown, grouped rather than dumped as raw text. The detail window title is the display name and the version, and it follows the selection.",
            Head("Uninstall"),
            "The tool relies on the stored UninstallString. It handles a path with spaces by taking it through the .exe, so C:\\Program Files is not split into C:\\Program.",
            "Forced uninstall is offered only for a Windows Installer package. Rather than trusting the stored command, it builds msiexec /x with a quiet or basic interface.",
            "If a package has set NoRemove, the module can clear it. That writes 0 to the value, and the write is logged under the account that did it before the uninstall is allowed to proceed.",
            Head("Search and filtering"),
            "The query bar is a live filter for the grid. A word with no field searches every column. For a precise filter, use a named field: app.displayname, app.publisher, app.version, app.InstallDate, and app.size.mb."
        ]);

    private static Draft Features()
        => new("Windows Features",
        [
            "Optional features are the components Windows can add or remove without a separate installer. Internet Information Services, the Hyper-V tools, and the legacy components all live here. The built-in dialog is Turn Windows features on or off. It reads the component store and presents a tree.",
            "A feature has a state. Enabled means it is on. Disabled means it is installed and off. Absent means it is not on the machine. A feature also has a parent. The tree is that parent relationship, which is why a group such as WCF Services contains children.",
            "The checkbox on a parent is tri-state because the parent is a summary of its children. Checked means the feature and everything under it are on. The middle state means some children are on. Clear means none are. A leaf is only ever checked or clear. Checking a child that needs its parent selects the parent too, so the summary that follows is not a surprise.",
            Head("How a change is applied"),
            "You check and clear on this form. Nothing is written yet. Apply Feature Changes opens an elevated worker, centered on this window. It lists exactly what will be enabled and what will be disabled. On continue it enables what was disabled, disables what was enabled, and suppresses a reboot where Windows allows it. The summary lists each feature and any error. The suite refreshes when it returns.",
            Head("Find"),
            "Expand and Collapse are on the menu, and so are Expand All and Collapse All. The expanded or collapsed state of the whole tree is remembered in Settings."
        ]);

    private static Draft Bloatware()
        => new("Bloatware",
        [
            "Inbox applications are AppX packages. They are not in the uninstall registry. They are registered with the AppX deployment service, and a provisioned copy can be kept so that every new user profile receives the package at first logon.",
            "This tab starts from a catalog, not from an inventory. The catalog is a known list of inbox and Store packages: the Bing apps, the Xbox components, the games that ship on a new image, Teams, and the ones Microsoft has since retired. Every catalog entry is shown. The Installed and Provisioned columns say whether this machine has it. A row with Installed = No is a package we know about, not a package that is here.",
            "Installed means the package is registered for this user and can be launched. Provisioned means the machine holds a copy for new users. Removing only the user registration leaves the provisioned copy, and the next person who logs on receives it again. A real removal takes both.",
            Head("How add and remove work"),
            "Remove is elevated. It removes the package for every user by its full package name, then removes the provisioned copy when the row is provisioned. Add looks for an AppxManifest.xml already on the disk, from a provisioned package or from another user's copy, and registers that. If there is no local manifest, it asks winget to fetch the package. If winget finds more than one, it installs the Store match that is not a classic or a plugin. If the package is already installed, that is a success. If Microsoft has retired it, the window says so instead of pretending a download exists.",
            Head("Find"),
            "Right-click a row. Add is shown when it is not installed. Remove is shown when it is."
        ]);

    private static Draft ActiveSetup()
        => new("Active Setup",
        [
            "Active Setup is a logon mechanism. It is not an installer and it is not a scheduled task. Windows compares a machine key with a per-user key at logon. If the machine version is higher, or the user has no key, Windows runs the command in StubPath and then writes the version into the user's key so it does not run again.",
            "The keys live under SOFTWARE\\Microsoft\\Active Setup\\Installed Components, in HKEY_LOCAL_MACHINE and HKEY_CURRENT_USER, and again under WOW6432Node for 32-bit. The key name is the component, usually a GUID. The values that matter are the display name, StubPath, Version, and IsInstalled. Version is compared as comma-separated numbers, not as text.",
            "Pending means the machine copy is ahead of this user's copy, so the command will run at the next logon. A component with no StubPath has nothing to run. That is normal for a component that only records a version.",
            "This area is rarely audited. A StubPath is an arbitrary command. Replacing one, or adding a new component, is a way to run code for every user at logon. That is why the tab exists.",
            Head("How a change is made"),
            "Add, Delete, and Clear command are on the menu. A user-key change is written directly. A machine-key change is handed to an elevated worker, which applies it and exits. There is no second confirm window. The summary is the result. Delete removes the key. Clear command removes StubPath and leaves the version, so the component stays but will not run.",
            Head("Find"),
            "The query bar uses the setup catalog. setup.stubpath IS NULL and setup.stubpath IS NOT NULL select components by whether they have a command."
        ]);

    private static Draft Dashboard()
        => new("Dashboard",
        [
            "The dashboard is a reading of the four inventories, not a fifth one. Each section is one inventory, with the counts that describe it and the charts that break it down.",
            "AppMgmtIQ is who published the software, how it is split between machine and user, when it was installed, and how large it is. Windows Features is how many are on and which parents own them. Bloatware is how much of the catalog is actually present. Active Setup is how much is armed to run, and whether it is a machine or a user component.",
            "A section with two charts shows them together. A section with more than two gets a vertical list, and the area shows the one you pick. Refresh rebuilds the charts from the inventories currently in memory."
        ]);

    private static Draft Exports()
        => new("Exports",
        [
            "Export writes a workbook from the inventories. The first sheet is a summary and is the sheet Excel opens on. Further sheets are added for the inventories you include: the package grid, the package details, the feature tree.",
            "Current filters exports what the grid is showing. All exports the inventory regardless of the query. Include details adds the registry values for each package, which is the same set the details window shows."
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
