namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

internal static class HelpCopy
{
    public static IReadOnlyList<Draft> Topics(string version, IReadOnlyList<string> license)
        =>
        [
            new("Overview",
            [
                "AppMgmtIQ is the installed-software view for a Windows machine.",
                "It reads what is installed, which Windows features are on, which catalog apps are present, and what Active Setup will run at the next logon.",
                Head("Where to find it"),
                "AppMgmtIQ is the package grid. Windows Features, Bloatware, and Active Setup are the other inventories. Export writes a workbook. Dashboard charts each inventory. Settings holds the defaults. Help is this page."
            ]),
            new("AppMgmtIQ",
            [
                "The package grid. One row is one installed package, from the four uninstall registry roots.",
                "Double-click a row for every registry value, on tabs. The title is the display name and version.",
                Head("Find"),
                "The query bar filters the grid. A word with no field searches every column. app.displayname, app.publisher, app.version, app.InstallDate, and app.size.mb are the named fields.",
                "Right-click a row for Product uninstall, the forced uninstall items, copy, and refresh."
            ]),
            new("Windows Features",
            [
                "The optional Windows features, in the same tree as Turn Windows features on or off.",
                "A parent checkbox is tri-state. Checked means the feature and its children are on. The middle state means some children are on. Clear means none are.",
                Head("Find"),
                "Check the features you want to change, then Apply Feature Changes on the menu. That opens the elevated worker. It summarizes what will be added and removed before it runs."
            ]),
            new("Bloatware",
            [
                "A catalog of known inbox and Store packages, marked with what this machine actually has.",
                "Installed means this user has it. Provisioned means the machine keeps a copy for every new user.",
                Head("Find"),
                "Right-click a row. Add is shown when it is not installed. Remove is shown when it is. Add registers a local copy, then asks winget, and says so when a package has been retired."
            ]),
            new("Active Setup",
            [
                "The Installed Components keys. A row with a command and a higher version runs at the next logon for a user who does not have it.",
                "Pending means the machine copy is ahead of this user's copy.",
                Head("Find"),
                "Right-click for Add, Delete, or Clear command. A machine change is elevated. A user change is not. The summary says what was written."
            ]),
            new("Dashboard",
            [
                "One section for each inventory. The sentence under the tabs says what that section is showing.",
                "Two charts sit side by side. More than two gets a vertical list, and the area shows the one you pick."
            ]),
            new("Exports",
            [
                "Writes the current inventories to a workbook. The summary sheet is the first sheet. AppMgmtIQ and Windows Features are added when you include them."
            ]),
            new("Settings",
            [
                "The defaults for this app. Show global and per-user packages, the date format, and the size decimal places.",
                "Settings are saved under C:\\ProgramData\\Vestigium\\Settings\\AppMgmtIQ. Logs are under C:\\ProgramData\\Vestigium\\Logs\\AppMgmtIQ."
            ]),
            new("License", license.Count == 0 ? ["The license file was empty."] : license, []),
            Glossary(),
            About(version)
        ];

    private static Draft Glossary()
        => new("Glossary",
            ["The words used on the grids."],
            [
                new("Packages",
                [
                    Term("Display name", "The name Windows shows. The details title uses it with the version."),
                    Term("Global", "Installed for the machine. HKLM. Per user is HKCU."),
                    Term("x64", "The native uninstall key. x86 is the WOW6432Node key."),
                    Term("System component", "Marked SystemComponent. Usually hidden from Add or Remove Programs."),
                    Term("NoRemove", "The package asked not to be removed. Clearing it is logged.")
                ]),
                new("Features",
                [
                    Term("Enabled", "The feature is on."),
                    Term("Disabled", "The feature is installed and off."),
                    Term("Absent", "The feature is not installed."),
                    Term("Parent", "The feature this one sits under in the tree.")
                ]),
                new("Bloatware",
                [
                    Term("Catalog", "The known list. A row here is not proof the app is installed."),
                    Term("Installed", "Registered for this user."),
                    Term("Provisioned", "Kept for every new user. Remove takes this too when the row is provisioned."),
                    Term("Retired", "Microsoft no longer offers the package. Add says so.")
                ]),
                new("Active Setup",
                [
                    Term("StubPath", "The command. It runs at logon when the version says it should."),
                    Term("Version", "Compared as comma-separated numbers. A higher machine version runs again."),
                    Term("Pending", "This user does not yet have the machine version."),
                    Term("Component", "The key name, usually a GUID.")
                ])
            ]);

    private static Draft About(string version)
        => new("About",
            ["AppMgmtIQ " + version + "."],
            [
                new("App",
                [
                    "AppMgmtIQ " + version + ". Installed packages, Windows features, catalog apps, and Active Setup.",
                    "Copyright (c) 2026 Nathaniel Wilkinson. MIT License. The notice is the License topic.",
                    "The public source for this host is Vestigium.Suite.Windows.",
                    Link("https://github.com/nwilkinsonlsnh/Vestigium.Suite.Windows", "Vestigium.Suite.Windows")
                ])
            ]);

    private static string Head(string line) => "\x1f" + line;
    private static string Link(string url, string label) => "\x1c" + url + "|" + label;
    private static string Term(string word, string means) => "\x1b" + word + "|" + means;
}

internal sealed record Draft(string Title, IReadOnlyList<string> Lines, IReadOnlyList<Draft>? Sections = null)
{
    public IReadOnlyList<Draft> Sections { get; init; } = Sections ?? [];
}
