namespace DistantSeas.Tracking.LogEntries;

public class LogEntryTotalPointsUpdate : LogEntry {
    public uint TotalPoints;

    public LogEntryTotalPointsUpdate() { }

    public LogEntryTotalPointsUpdate(uint totalPoints) {
        this.TotalPoints = totalPoints;
    }
}
