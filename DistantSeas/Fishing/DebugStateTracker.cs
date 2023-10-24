using System.Collections.Generic;
using DistantSeas.SpreadsheetSpaghetti.Types;

namespace DistantSeas.Fishing;

public class DebugStateTracker : IStateTracker {
    public bool IsInOceanFishing { get; set; } = true;
    public bool IsDataLoaded { get; set; } = true;

    public uint Points { get; set; } = 0;
    public uint TotalPoints { get; set; } = 0;
    public uint CurrentRoute { get; set; } = 1;
    public byte CurrentZone { get; set; } = 0;
    public WeatherType CurrentWeather { get; set; } = WeatherType.ClearSkies;
    public float TimeLeftInZone { get; set; } = 60;
    public bool IsSpectralActive { get; set; } = false;

    public List<MissionState> MissionState { get; set; } = new() {
        new MissionState(0),
        new MissionState(0),
        new MissionState(0)
    };

    public bool HasActionUnlocked(uint id) {
        return true;
    }

    public bool IsActionReady(uint id) {
        return true;
    }

    public uint GetStatusStacks(uint id) {
        return 1;
    }

    public int GetItemCount(uint id) {
        return 42069;
    }

    public void Dispose() { }

    public void ResetMissions() {
        MissionState = new() {
            new MissionState(0),
            new MissionState(0),
            new MissionState(0)
        };
    }
}
