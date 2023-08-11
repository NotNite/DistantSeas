using System.Collections.Generic;
using DistantSeas.Fishing;

namespace DistantSeas.Tracking.LogEntries;

public class LogEntryEnterBoat : LogEntry {
    public uint Route;
    public List<MissionState> Missions;

    public LogEntryEnterBoat() { }

    public LogEntryEnterBoat(IStateTracker stateTracker) {
        this.Route = stateTracker.CurrentRoute;
        this.Missions = stateTracker.MissionState;
    }
}
