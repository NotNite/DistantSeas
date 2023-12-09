using DistantSeas.Common;
using DistantSeas.Fishing;

namespace DistantSeas.Tracking.LogEntries;

public class LogEntryZoneChanged : LogEntry {
    public uint Zone;
    public Time? CurrentTime;
    public WeatherType CurrentWeather;

    public LogEntryZoneChanged() { }

    public LogEntryZoneChanged(IStateTracker stateTracker) {
        this.Zone = stateTracker.CurrentZone;
        this.CurrentTime = Plugin.BaitManager.GetCurrentTime();
        this.CurrentWeather = stateTracker.CurrentWeather;
    }
}
