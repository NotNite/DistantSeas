using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CheapLoc;

namespace DistantSeas.Core;

public class LocalizationManager {
    public static List<string> AvailableLanguages = new() {"en", "nl", "ja", "es"};

    public static Dictionary<string, string> CodesToNames = AvailableLanguages
                                                            .Select(x => (x, GetName(x)))
                                                            .ToDictionary(x => x.x, x => x.Item2);

    public LocalizationManager() {
        this.Setup();
    }

    public void Setup() {
        var lang = Plugin.Configuration.LanguageOverride ?? Plugin.PluginInterface.UiLanguage;
        var path = Plugin.ResourceManager.GetDataPath("loc", lang + ".json");
        var assembly = Plugin.ResourceManager.DistantSeasAssembly;

        if (File.Exists(path)) {
            var loc = File.ReadAllText(path);
            Loc.Setup(loc, assembly);
        } else {
            Loc.SetupWithFallbacks(assembly);
        }
    }

    public void Export() {
        Loc.ExportLocalizableForAssembly(Plugin.ResourceManager.DistantSeasAssembly);
    }

    // Stolen from Dalamud
    private static string GetName(string code) {
        var name = CultureInfo.GetCultureInfo(code).NativeName;
        return char.ToUpper(name[0]) + name[1..];
    }
}
