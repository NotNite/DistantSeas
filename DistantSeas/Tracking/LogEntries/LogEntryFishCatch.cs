namespace DistantSeas.Tracking.LogEntries;

public class LogEntryFishCatch : LogEntry {
    public uint Item;
    public bool Large;
    public uint Points;

    public LogEntryFishCatch() { }

    public LogEntryFishCatch(uint item, uint large, uint points) {
        this.Item = item;
        this.Large = large > 0;
        this.Points = points;
    }
}
