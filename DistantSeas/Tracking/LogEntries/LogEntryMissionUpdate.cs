using DistantSeas.Fishing;

namespace DistantSeas.Tracking.LogEntries;

public class LogEntryMissionUpdate : LogEntry {
    public MissionState State;

    public LogEntryMissionUpdate() { }

    public LogEntryMissionUpdate(MissionState state) {
        this.State = state;
    }
}
