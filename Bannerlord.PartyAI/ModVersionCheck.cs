using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Bannerlord.PartyAI;

internal static class ModVersionCheck
{
    private static bool _shown;

    internal static void DisplayStartupMessagesIfNeeded()
    {
        if (_shown)
        {
            return;
        }

        _shown = true;

        try
        {
            TaleWorlds.Library.InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=PAIVersionLoaded}Loaded Party AI Controls Reforged.").ToString(),
                Colors.Green));

            string builtForVersion = GetBuiltForGameVersion();
            if (string.IsNullOrEmpty(builtForVersion))
            {
                return;
            }

            ApplicationVersion gameVersion = ApplicationVersion.FromParametersFile();
            if (VersionsMatch(gameVersion, builtForVersion))
            {
                return;
            }

            TextObject warning = new TextObject("{=PAIVersionMismatch}The game version is {GAME_VERSION}. The Party AI Controls version installed is made for {MOD_VERSION}. There may be compatibility issues, recommend checking latest mod version.");
            warning.SetTextVariable("GAME_VERSION", FormatVersion(gameVersion));
            warning.SetTextVariable("MOD_VERSION", NormalizeVersionPrefix(builtForVersion));

            TaleWorlds.Library.InformationManager.DisplayMessage(new InformationMessage(warning.ToString(), Colors.Red));
        }
        catch
        {
            // Never block game startup on version-check failures.
        }
    }

    private static string GetBuiltForGameVersion()
    {
        return typeof(SubModule).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "GameVersion")
            ?.Value ?? string.Empty;
    }

    private static bool VersionsMatch(ApplicationVersion gameVersion, string builtForVersion)
    {
        // FromString expects "v{major}.{minor}.{revision}" — the leading type prefix is required.
        ApplicationVersion modVersion = ApplicationVersion.FromString(NormalizeVersionPrefix(builtForVersion));
        return gameVersion.Major == modVersion.Major
            && gameVersion.Minor == modVersion.Minor
            && gameVersion.Revision == modVersion.Revision;
    }

    private static string FormatVersion(ApplicationVersion version) =>
        $"v{version.Major}.{version.Minor}.{version.Revision}";

    private static string NormalizeVersionPrefix(string version) =>
        version.StartsWith("v") || version.StartsWith("e") || version.StartsWith("a")
            ? version
            : $"v{version}";
}
