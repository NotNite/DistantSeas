using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using DistantSeas.Common;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace DistantSeas.Fishing;

public class BaitManager : IDisposable {
    private ExcelSheet<IKDRoute> ikdRoute;

    private delegate byte ExecuteCommandDelegate(int id, int unk1, uint baitId, int unk2, int unk3);
    private ExecuteCommandDelegate executeCommand;

    public uint CurrentBait = 0;

    public uint? OverrideZone = null;
    public uint CurrentZone => this.OverrideZone ?? Plugin.StateTracker.CurrentZone;

    public BaitManager() {
        this.ikdRoute = Plugin.DataManager.Excel.GetSheet<IKDRoute>()!;

        // Stolen from my Simple Tweaks bait command tweak:
        // https://github.com/Caraxi/SimpleTweaksPlugin/blob/e09b4ae7b879fa7db7a9e6091a1d79067513dd39/Tweaks/BaitCommand.cs#L26C17-L26C40
        var executeCommandPtr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 8D 46 0A");
        executeCommand = Marshal.GetDelegateForFunctionPointer<ExecuteCommandDelegate>(executeCommandPtr);

        Plugin.Framework.Update += this.FrameworkUpdate;
    }

    public void Dispose() {
        Plugin.Framework.Update -= this.FrameworkUpdate;
    }

    private unsafe void FrameworkUpdate(IFramework framework) {
        this.CurrentBait = UIState.Instance()->PlayerState.FishingBait;

        var tracker = Plugin.StateTracker;
        if (
            tracker is {IsInOceanFishing: true, IsDataLoaded: true}
            && Plugin.BaitManager.OverrideZone == tracker.CurrentZone
        ) {
            Plugin.BaitManager.OverrideZone = null;
        }
    }

    public Time? GetCurrentTime() {
        var route = this.ikdRoute.GetRow(Plugin.StateTracker.CurrentRoute)!;
        var time = route.Time[(int) this.CurrentZone].Value.RowId;
        return time == 0 ? null : (Time) time;
    }

    public Spot GetCurrentSpot() {
        var route = this.ikdRoute.GetRow(Plugin.StateTracker.CurrentRoute)!;
        var spotId = route.Spot[(int) this.CurrentZone].RowId;
        return Plugin.FishData.Spots.First(
            x => x.Type == (SpotType) spotId && x.IsSpectral == Plugin.StateTracker.IsSpectralActive
        );
    }

    public List<Fish> GetAvailableFish() {
        var spot = this.GetCurrentSpot();
        return spot.Fish.Where(x => this.CanCatchFish(spot, x)).ToList();
    }

    public List<uint> GetBaitChain(uint fishId) {
        var spot = this.GetCurrentSpot();
        var fish = spot.Fish.FirstOrDefault(x => x.ItemId == fishId);
        if (fish == null) {
            // Probably passed a bait ID to GetBaitChain, just return empty
            return new List<uint>();
        }

        var baitChain = new List<uint> {fishId};
        if (fish.Mooch != null) {
            var mooch = fish.GetMoochFish(spot);
            if (mooch != null) {
                var baitRequirements = this.GetBaitChain(mooch.ItemId);
                baitRequirements.Reverse(); // un-reverse it so we can reverse it ourselves
                baitChain.AddRange(baitRequirements);
            }
        } else {
            var bestBait = this.BestBaitForFish(spot, fish);
            if (bestBait != null) baitChain.Add(bestBait.Value);
        }

        baitChain.Reverse();
        return baitChain;
    }

    private uint? BestBaitForFish(Spot spot, Fish fish) {
        if (fish.RequiredBait != null) return fish.RequiredBait;
        var bestBait = fish.BiteTimes.ToList();

        if (!Plugin.Configuration.PreferDynamicSuggestions) {
            bestBait = bestBait
                       .Where(x => x.Value.CellType == CellType.BestOrRequired)
                       .ToList();
        }

        if (Plugin.Configuration.OceanFishingBaitsOnly) {
            var newBestBait = bestBait
                              .Where(x => x.Key is Utils.Ragworm or Utils.Krill or Utils.PlumpWorm)
                              .ToList();
            if (newBestBait.Any()) bestBait = newBestBait;
        }

        if (!bestBait.Any()) return fish.BiteTimes.FirstOrDefault().Key;
        if (bestBait.Count == 1) return bestBait.First().Key;

        // Find the one with least overlap for other fish
        (uint, int)? leastOverlap = null;
        var end = 45; // picked arbitrarily
        foreach (var baitId in bestBait.Select(x => x.Key)) {
            var range = fish.BiteTimes[baitId].Range;
            var rangeEnd = range.End ?? end;
            var overlapTotal = 0;

            foreach (var otherFish in spot.Fish) {
                if (!otherFish.BiteTimes.ContainsKey(baitId)) continue;
                if (!this.CanCatchFish(spot, otherFish)) continue;

                var otherRange = otherFish.BiteTimes[baitId].Range;
                var otherRangeEnd = otherRange.End ?? end;
                var overlap = Math.Max(0, Math.Min(rangeEnd, otherRangeEnd) - Math.Max(range.Start, otherRange.Start));
                overlapTotal += overlap;
            }

            if (leastOverlap == null || overlapTotal < leastOverlap.Value.Item2) {
                leastOverlap = (baitId, overlapTotal);
            }
        }

        if (leastOverlap != null) {
            return leastOverlap.Value.Item1;
        } else {
            // give up
            return bestBait.First().Key;
        }
    }

    private bool CanCatchFish(Spot spot, Fish fish) {
        if (fish.Intuition != null) {
            foreach (var (id, _) in fish.Intuition.Fish) {
                var intFish = spot.Fish.First(x => x.ItemId == id);
                if (!this.CanCatchFish(spot, intFish)) return false;
            }
        }

        var moochFish = fish.GetMoochFish(spot);
        if (moochFish != null && !this.CanCatchFish(spot, moochFish)) return false;

        var time = Plugin.BaitManager.GetCurrentTime();
        var weather = Plugin.StateTracker.CurrentWeather;
        var availableInTime = time == null || fish.IsAvailableInTime(time.Value);
        var availableInWeather = this.OverrideZone == null || fish.IsAvailableInWeather(weather);

        return availableInTime && availableInWeather;
    }

    public void SetCurrentBait(uint id) {
        if (Plugin.StateTracker.GetItemCount(id) <= 0) {
            Plugin.PluginLog.Debug("Tried to set bait to {BaitId}, but has none!", id);
            return;
        }

        if (!this.CanChangeBait()) {
            Plugin.PluginLog.Debug("Tried to set bait to {BaitId}, but can't!", id);
            return;
        }

        if (!Utils.IsBait(id)) {
            Plugin.PluginLog.Debug("Tried to set bait to {BaitId}, but it's not bait!", id);
            return;
        }

        if (this.CurrentBait == id) {
            Plugin.PluginLog.Debug("Tried to set bait to {BaitId}, but it's already our bait!", id);
            return;
        }

        this.executeCommand(701, 4, id, 0, 0);
    }

    public unsafe bool CanChangeBait() {
        // fucking dalamud apis being broken
        var chara = (Character*) Plugin.ObjectTable.LocalPlayer?.Address;
        if (chara == null) return false;

        return chara->CharacterData.ClassJob == 18 &&      // FSH
               !Plugin.Condition[ConditionFlag.Fishing] && // not actively fishing
               !Plugin.Condition[ConditionFlag.Occupied];  // in between zones
    }
}
