using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using DistantSeas.Common;
using DistantSeas.Tracking.LogEntries;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

// ReSharper disable InconsistentNaming

namespace DistantSeas.Tracking;

public unsafe class Journal : IDisposable {
    private List<LogEntry> entries = new();
    public bool IsLogging => this.entries.Any();
    public int EntryCount => this.entries.Count;

    private uint points = 0;
    private uint totalPoints = 0;

    private uint zone = 0;
    private WeatherType weather = 0;

    private bool spectral = false;
    private uint missionOne = 0;
    private uint missionTwo = 0;
    private uint missionThree = 0;

    private bool pollingForDataPopulation = false;

    public delegate nint OnIKDFishCaughtDelegate(nint a1, IKDFishCatch* fishCatch);

    [Signature(
        "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 8B 72 ?? 48 8B FA 48 8B D9",
        UseFlags = SignatureUseFlags.Hook,
        DetourName = nameof(OnIKDFishCaughtDetour)
    )]
    public Hook<OnIKDFishCaughtDelegate> OnIKDFishCaughtHook = null!;

    public Journal() {
        Plugin.GameInteropProvider.InitializeFromAttributes(this);
        this.OnIKDFishCaughtHook.Enable();

        Plugin.Framework.Update += this.FrameworkUpdate;
        Plugin.EnteredOceanFishing += this.EnteredOceanFishing;
        Plugin.ExitedOceanFishing += this.ExitedOceanFishing;
    }

    public void Dispose() {
        this.OnIKDFishCaughtHook.Dispose();

        Plugin.Framework.Update -= this.FrameworkUpdate;
        Plugin.EnteredOceanFishing -= this.EnteredOceanFishing;
        Plugin.ExitedOceanFishing -= this.ExitedOceanFishing;
    }

    private void FrameworkUpdate(IFramework framework) {
        var result = AgentModule.Instance()->GetAgentIKDResult();
        if (result != null && result->Data != null) {
            var newTotalPoints = result->Data->TotalScore;
            if (newTotalPoints > this.totalPoints) {
                this.totalPoints = newTotalPoints;
                this.entries.Add(new LogEntryTotalPointsUpdate(this.totalPoints));
            }
        }

        var stateTracker = Plugin.StateTracker;
        if (stateTracker is null) return;
        if (stateTracker.IsInOceanFishing) {
            var newPoints = stateTracker.Points;
            if (newPoints > this.points) {
                this.points = newPoints;
                this.entries.Add(new LogEntryPointsUpdate(this.points));
            }

            var newSpectral = stateTracker.IsSpectralActive;
            if (newSpectral != this.spectral) {
                this.spectral = newSpectral;
                this.entries.Add(
                    this.spectral
                        ? new LogEntrySpectralStarted()
                        : new LogEntrySpectralEnded()
                );
            }


            // Only bother updating when zone data is populated
            if (Plugin.StateTracker.IsDataLoaded) {
                var newZone = stateTracker.CurrentZone;
                var newWeather = stateTracker.CurrentWeather;

                if (newZone != this.zone) {
                    this.zone = newZone;
                    this.weather = newWeather; // also update weather too on zone change
                    this.entries.Add(new LogEntryZoneChanged(stateTracker));
                }

                // specifically in case weather changes mid-zone, not sure if it's possible
                if (newWeather != this.weather) {
                    this.weather = newWeather;
                    this.entries.Add(new LogEntryZoneChanged(stateTracker));
                }
            }

            if (stateTracker.IsDataLoaded) {
                var one = stateTracker.MissionState[0];
                var two = stateTracker.MissionState[1];
                var three = stateTracker.MissionState[2];

                // this sucks lmao
                if (one.Progress > this.missionOne) {
                    this.missionOne = one.Progress;
                    this.entries.Add(new LogEntryMissionUpdate(one));
                }

                if (two.Progress > this.missionTwo) {
                    this.missionTwo = two.Progress;
                    this.entries.Add(new LogEntryMissionUpdate(two));
                }

                if (three.Progress > this.missionThree) {
                    this.missionThree = three.Progress;
                    this.entries.Add(new LogEntryMissionUpdate(three));
                }

                // We enter the zone before the data is populated
                // We can tell when it's populated when the mission states are set
                if (this.pollingForDataPopulation && stateTracker.IsDataLoaded) {
                    this.pollingForDataPopulation = false;
                    this.entries.Insert(0, new LogEntryHeader());
                    this.entries.Insert(1, new LogEntryEnterBoat(stateTracker));
                }
            }
        }
    }

    private void EnteredOceanFishing() {
        this.pollingForDataPopulation = true;
    }

    private void ExitedOceanFishing() {
        this.entries.Add(new LogEntryExitBoat());
        this.SaveToFile();
        this.entries.Clear();

        Plugin.AchievementTracker.AddPoints(this.totalPoints);

        this.points = 0;
        this.totalPoints = 0;
        this.spectral = false;
        this.missionOne = 0;
        this.missionTwo = 0;
        this.missionThree = 0;
        this.pollingForDataPopulation = false;

        Plugin.StateTracker.ResetMissions();
    }

    // public for debugging purposes
    public void SaveToFile() {
        // Don't bother writing if the journal is disabled
        // This still logs, just doesn't write them to a file, but whatever
        if (!Plugin.Configuration.JournalEnabled) return;

        var filename = "DistantSeas-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".json";
        var path = Plugin.ResourceManager.GetConfigPath("logs", filename);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var json = Serializer.ToString(this.entries);
        File.WriteAllText(path, json);
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public struct IKDFishCatch {
        [FieldOffset(0x00)] public uint ItemId;
        [FieldOffset(0x06)] public byte Average;
        [FieldOffset(0x0A)] public byte Large;
        [FieldOffset(0x0C)] public ushort Points;
    }

    public nint OnIKDFishCaughtDetour(nint a1, IKDFishCatch* fishCatch) {
        this.entries.Add(new LogEntryFishCatch(fishCatch->ItemId, fishCatch->Large, fishCatch->Points));
        return this.OnIKDFishCaughtHook.Original(a1, fishCatch);
    }
}
