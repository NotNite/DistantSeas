namespace DistantSeas.Tracking.LogEntries;

public class LogEntryPointsUpdate : LogEntry {
    public uint Points;

    public LogEntryPointsUpdate() { }

    public LogEntryPointsUpdate(uint points) {
        this.Points = points;
    }
}
