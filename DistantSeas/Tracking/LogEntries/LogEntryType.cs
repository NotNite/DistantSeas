using System.Text.Json.Serialization;

namespace DistantSeas.Tracking.LogEntries;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LogEntryType {
    Header,
    
    EnterBoat,
    ExitBoat,
    ZoneChanged,

    SpectralStarted,
    SpectralEnded,

    PointsUpdate,
    TotalPointsUpdate,
    FishCatch,
    MissionUpdate
}
