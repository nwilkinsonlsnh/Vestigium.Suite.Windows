namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public static class BloatwareCatalog
{
    public static IReadOnlyList<BloatwareEntry> Entries { get; } =
    [
        Entry("Microsoft.BingNews", "News", "Store"),
        Entry("Microsoft.BingWeather", "Weather", "Store"),
        Entry("Microsoft.BingSearch", "Bing Search", "Store"),
        Entry("Microsoft.StartExperiencesApp", "Start Experiences", "Store"),
        Entry("Microsoft.WindowsMaps", "Maps", "Store"),
        Entry("Microsoft.GetHelp", "Get Help", "System"),
        Entry("Microsoft.Getstarted", "Tips", "System"),
        Entry("Microsoft.WindowsFeedbackHub", "Feedback Hub", "System"),
        Entry("Microsoft.MixedReality.Portal", "Mixed Reality Portal", "System"),
        Entry("Microsoft.OneConnect", "Mobile Plans", "System"),
        Entry("Microsoft.Wallet", "Wallet", "System"),
        Entry("Microsoft.Print3D", "Print 3D", "System"),
        Entry("Microsoft.3DBuilder", "3D Builder", "System"),
        Entry("Microsoft.Microsoft3DViewer", "3D Viewer", "System"),
        Entry("Microsoft.MSPaint", "Paint 3D", "System"),
        Entry("Microsoft.549981C3F5F10", "Cortana", "System"),
        Entry("Microsoft.Windows.DevHome", "Dev Home", "System"),
        Entry("Microsoft.Copilot", "Copilot", "System"),
        Entry("Microsoft.XboxApp", "Xbox", "Xbox"),
        Entry("Microsoft.GamingApp", "Xbox", "Xbox"),
        Entry("Microsoft.Xbox.TCUI", "Xbox TCUI", "Xbox"),
        Entry("Microsoft.XboxGameOverlay", "Xbox Game Bar", "Xbox"),
        Entry("Microsoft.XboxGamingOverlay", "Xbox Game Bar", "Xbox"),
        Entry("Microsoft.XboxIdentityProvider", "Xbox Identity", "Xbox"),
        Entry("Microsoft.XboxSpeechToTextOverlay", "Xbox Speech to Text", "Xbox"),
        Entry("Microsoft.ZuneMusic", "Media Player", "Media"),
        Entry("Microsoft.ZuneVideo", "Movies and TV", "Media"),
        Entry("Microsoft.WindowsSoundRecorder", "Voice Recorder", "Media"),
        Entry("Clipchamp.Clipchamp", "Clipchamp", "Media"),
        Entry("SpotifyAB.SpotifyMusic", "Spotify", "Media"),
        Entry("Disney.37853FC22B2CE", "Disney", "Media"),
        Entry("Microsoft.MicrosoftSolitaireCollection", "Solitaire Collection", "Games"),
        Entry("king.com.CandyCrushSaga", "Candy Crush Saga", "Games"),
        Entry("king.com.CandyCrushSodaSaga", "Candy Crush Soda", "Games"),
        Entry("king.com.FarmHeroesSaga", "Farm Heroes Saga", "Games"),
        Entry("Microsoft.HiddenCity", "Hidden City", "Games"),
        Entry("A278AB0D.MarchofEmpires", "March of Empires", "Games"),
        Entry("Microsoft.MicrosoftOfficeHub", "Office", "Productivity"),
        Entry("Microsoft.People", "People", "Productivity"),
        Entry("Microsoft.YourPhone", "Phone Link", "Productivity"),
        Entry("Microsoft.SkypeApp", "Skype", "Productivity"),
        Entry("Microsoft.MicrosoftStickyNotes", "Sticky Notes", "Productivity"),
        Entry("Microsoft.Todos", "To Do", "Productivity"),
        Entry("Microsoft.PowerAutomateDesktop", "Power Automate", "Productivity"),
        Entry("MicrosoftTeams", "Teams", "Productivity", "MSTeams"),
        Entry("Microsoft.OutlookForWindows", "Outlook", "Productivity"),
        Entry("Microsoft.WindowsCommunicationsApps", "Mail and Calendar", "Productivity"),
        Entry("Microsoft.Messaging", "Messaging", "Productivity"),
        Entry("Facebook.Facebook", "Facebook", "Store")
    ];

    private static BloatwareEntry Entry(string family, string name, string category, params string[] aliases) =>
        new(family, name, category, aliases);

}

public sealed record BloatwareEntry(string Family, string Name, string Category, params string[] Aliases)
{
    public bool Matches(string family)
        => Family.Equals(family, StringComparison.OrdinalIgnoreCase)
           || Aliases.Any(alias => alias.Equals(family, StringComparison.OrdinalIgnoreCase));
}
