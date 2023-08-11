namespace DistantSeas.Tracking.LogEntries;

public class LogEntryHeader : LogEntry {
    public uint Version = 1;
    public string PluginVersion = Plugin.ResourceManager.DistantSeasAssembly.GetName().Version!.ToString();
}
