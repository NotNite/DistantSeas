using System.IO;
using System.Reflection;

namespace DistantSeas.Core; 

public class ResourceManager {
    public Assembly DistantSeasAssembly => Assembly.GetAssembly(typeof(Plugin))!;
    private string dataDir = Plugin.PluginInterface.AssemblyLocation.DirectoryName!;
    private string configDir = Plugin.PluginInterface.GetPluginConfigDirectory();
    
    public string GetDataPath(params string[] paths) {
        var arr = new string[paths.Length + 1];
        arr[0] = this.dataDir;
        paths.CopyTo(arr, 1);
        return Path.Combine(arr);
    }
    
    public string GetConfigPath(params string[] paths) {
        var arr = new string[paths.Length + 1];
        arr[0] = this.configDir;
        paths.CopyTo(arr, 1);
        return Path.Combine(arr);
    }
}
