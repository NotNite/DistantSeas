using System.IO;
using CheapLoc;

namespace DistantSeas.Core;

public class LocalizationManager {
    public LocalizationManager() {
        var lang = Plugin.PluginInterface.UiLanguage;
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
}
