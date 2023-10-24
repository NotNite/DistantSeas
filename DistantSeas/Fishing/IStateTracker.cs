using System;
using System.Collections.Generic;
using DistantSeas.SpreadsheetSpaghetti.Types;

namespace DistantSeas.Fishing;

public interface IStateTracker : IDisposable {
    public bool IsInOceanFishing { get; }
    public bool IsDataLoaded { get; }

    public uint Points { get; }

    public uint CurrentRoute { get; }
    public byte CurrentZone { get; }
    public WeatherType CurrentWeather { get; }
    public float TimeLeftInZone { get; }
    public bool IsSpectralActive { get; }
    public List<MissionState> MissionState { get; }

    public bool HasActionUnlocked(uint id);
    public bool IsActionReady(uint id);
    public uint GetStatusStacks(uint id);
    public int GetItemCount(uint id);
    public void ResetMissions();
}
