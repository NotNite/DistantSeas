using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using DistantSeas.Common;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Action = Lumina.Excel.Sheets.Action;

namespace DistantSeas.Fishing;

public unsafe class NormalStateTracker : IStateTracker {
    public bool IsInOceanFishing { get; set; } = false;

    public bool IsDataLoaded => this.MissionState.Count > 0
                                && this.MissionState[0].Row != 0
                                // this is a bad place to put these checks but whatevs
                                && Plugin.BaitManager.GetCurrentTime() != null;

    public uint Points { get; set; } = 0;
    public uint TotalPoints { get; set; } = 0;
    public uint Gp { get; set; } = 0;
    public uint MaxGp { get; set; } = 0;
    public uint TimeUntilGpFull { get; set; } = 0;
    public uint CurrentRoute { get; set; } = 0;
    public byte CurrentZone { get; set; } = 0;
    public WeatherType CurrentWeather { get; set; } = WeatherType.Unknown;
    public float TimeLeftInZone { get; set; } = 0;
    public bool IsSpectralActive { get; set; } = false;
    public List<MissionState> MissionState { get; set; } = new();

    private ExcelSheet<Action> actionSheet;
    private List<uint> oceanFishingZones;

    public NormalStateTracker() {
        this.actionSheet = Plugin.DataManager.GetExcelSheet<Action>()!;

        this.oceanFishingZones = new();
        var territoryType = Plugin.DataManager.GetExcelSheet<TerritoryType>()!;
        foreach (var row in territoryType) {
            if (row.TerritoryIntendedUse.RowId == 46) {
                this.oceanFishingZones.Add(row.RowId);
            }
        }

        Plugin.Framework.Update += this.FrameworkUpdate;
    }

    public void Dispose() {
        Plugin.Framework.Update -= this.FrameworkUpdate;
    }

    public bool HasActionUnlocked(uint id) {
        var row = actionSheet.GetRow(id)!;
        return UIState.Instance()->IsUnlockLinkUnlocked(row.UnlockLink.RowId);
    }

    public bool IsActionReady(uint id) {
        var row = actionSheet.GetRow(id)!;
        var type = (ActionType) row.CastType;
        return ActionManager.Instance()->IsActionOffCooldown(type, id);
    }


    public uint GetStatusStacks(uint id) {
        var list = Plugin.ObjectTable.LocalPlayer!.StatusList;
        var entry = list.FirstOrDefault(x => x.StatusId == id);
        return entry?.Param ?? (uint) 0;
    }

    public int GetItemCount(uint id) {
        return InventoryManager.Instance()->GetInventoryItemCount(id);
    }

    private void FrameworkUpdate(IFramework framework) {
        this.NukeAddonsFromOrbit();

        var teri = Plugin.ClientState.TerritoryType;
        if (teri != 0) {
            var inOceanFishing = this.oceanFishingZones.Contains(teri);
            if (inOceanFishing != this.IsInOceanFishing) {
                // Do this instead of TerritoryChanged event in case the plugin is enabled/reloaded inside of a boat
                this.IsInOceanFishing = inOceanFishing;
                Plugin.DispatchEnterExit(this.IsInOceanFishing);
            }
        }

        var agentModule = AgentModule.Instance();
        var fishingLog = agentModule->GetAgentIKDFishingLog();
        if (fishingLog != null) {
            this.Points = fishingLog->Points;
        }

        var result = agentModule->GetAgentIKDResult();
        if (result != null && result->Data != null) {
            this.TotalPoints = result->Data->TotalScore;
        }

        var localPlayer = Plugin.ObjectTable.LocalPlayer;
        if (localPlayer != null) {
            this.Gp = localPlayer.CurrentGp;
            this.MaxGp = localPlayer.MaxGp;
            this.TimeUntilGpFull = 0; // TODO steal from st
        }

        var oceanFishing = EventFramework.Instance()->GetInstanceContentOceanFishing();
        if (oceanFishing != null) {
            this.CurrentRoute = oceanFishing->CurrentRoute;
            this.CurrentZone = (byte) oceanFishing->CurrentZone;

            var weather = EnvManager.Instance()->ActiveWeather;
            this.CurrentWeather = (WeatherType) weather;

            this.TimeLeftInZone = oceanFishing->InstanceContentDirector.ContentDirector.ContentTimeLeft -
                                  oceanFishing->TimeOffset;

            this.IsSpectralActive = oceanFishing->SpectralCurrentActive;

            if (oceanFishing->Mission1Type != 0) {
                if (this.MissionState.Count <= 0) {
                    this.MissionState = new List<MissionState> {
                        new(oceanFishing->Mission1Type),
                        new(oceanFishing->Mission2Type),
                        new(oceanFishing->Mission3Type)
                    };
                }

                this.MissionState[0].Progress = oceanFishing->Mission1Progress;
                this.MissionState[1].Progress = oceanFishing->Mission2Progress;
                this.MissionState[2].Progress = oceanFishing->Mission3Progress;
            }
        }
    }

    private void NukeAddonsFromOrbit() {
        if (Plugin.Configuration.HideVanillaOverlay) {
            var fishingLog = (AtkUnitBase*) Plugin.GameGui.GetAddonByName("IKDFishingLog").Address;
            if (fishingLog != null && fishingLog->IsVisible) {
                fishingLog->IsVisible = false;
            }

            var mission = (AtkUnitBase*) Plugin.GameGui.GetAddonByName("IKDMission").Address;
            if (mission != null && mission->IsVisible) {
                mission->IsVisible = false;
            }
        }
    }

    public void ResetMissions() {
        MissionState.Clear();
    }
}
