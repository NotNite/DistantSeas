using System;
using System.Text.Json.Serialization;

namespace DistantSeas.Tracking.LogEntries;

// jesus christ they weren't lying system.text.json polymorphism sucks
[JsonDerivedType(typeof(LogEntryHeader), typeDiscriminator: nameof(LogEntryType.Header))]
[JsonDerivedType(typeof(LogEntryEnterBoat), typeDiscriminator: nameof(LogEntryType.EnterBoat))]
[JsonDerivedType(typeof(LogEntryExitBoat), typeDiscriminator: nameof(LogEntryType.ExitBoat))]
[JsonDerivedType(typeof(LogEntryZoneChanged), typeDiscriminator: nameof(LogEntryType.ZoneChanged))]
[JsonDerivedType(typeof(LogEntrySpectralStarted), typeDiscriminator: nameof(LogEntryType.SpectralStarted))]
[JsonDerivedType(typeof(LogEntrySpectralEnded), typeDiscriminator: nameof(LogEntryType.SpectralEnded))]
[JsonDerivedType(typeof(LogEntryPointsUpdate), typeDiscriminator: nameof(LogEntryType.PointsUpdate))]
[JsonDerivedType(typeof(LogEntryTotalPointsUpdate), typeDiscriminator: nameof(LogEntryType.TotalPointsUpdate))]
[JsonDerivedType(typeof(LogEntryFishCatch), typeDiscriminator: nameof(LogEntryType.FishCatch))]
[JsonDerivedType(typeof(LogEntryMissionUpdate), typeDiscriminator: nameof(LogEntryType.MissionUpdate))]
public abstract class LogEntry {
    public DateTime Time = DateTime.Now;
}
