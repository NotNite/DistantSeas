using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using DistantSeas.Common;
using DistantSeas.Core;
using DistantSeas.Fishing;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using Weather = Lumina.Excel.GeneratedSheets.Weather;

namespace DistantSeas.Windows.Main;

public unsafe class DebugSection : MainWindowSection {
    private RawExcelSheet ikdPlayerMissionCondition;
    private ExcelSheet<IKDRoute> ikdRoute;
    private ExcelSheet<IKDSpot> ikdSpot;
    private ExcelSheet<Item> item;
    private ExcelSheet<Weather> weather;

    public DebugSection() : base(
        FontAwesomeIcon.Bug,
        "DEBUG",
        MainWindowCategory.Debug
    ) {
        this.ikdPlayerMissionCondition = Plugin.DataManager.Excel.GetSheetRaw("IKDPlayerMissionCondition")!;
        this.ikdRoute = Plugin.DataManager.Excel.GetSheet<IKDRoute>()!;
        this.ikdSpot = Plugin.DataManager.Excel.GetSheet<IKDSpot>()!;
        this.item = Plugin.DataManager.Excel.GetSheet<Item>()!;
        this.weather = Plugin.DataManager.Excel.GetSheet<Weather>()!;
    }

    public override void Draw() {
        using (ImRaii.TabBar("##DistantSeasDebug")) {
            using (var tab = ImRaii.TabItem("IKD")) {
                if (tab.Success) this.DrawIkd();
            }

            using (var tab = ImRaii.TabItem("FishDataManager")) {
                if (tab.Success) this.DrawFishDataManager();
            }

            using (var tab = ImRaii.TabItem("Misc")) {
                if (tab.Success) this.DrawMisc();
            }

            using (var tab = ImRaii.TabItem("Patterns")) {
                if (tab.Success) this.DrawPatterns();
            }

            using (var tab = ImRaii.TabItem("State tracker")) {
                if (tab.Success) this.DrawStateTracker();
            }

            using (var tab = ImRaii.TabItem("Journal")) {
                if (tab.Success) this.DrawJournal();
            }
        }
    }

    private void DrawIkd() {
        this.DrawDirector();
        ImGui.Separator();
        this.DrawFishingLog();
        ImGui.Separator();
        this.DrawResult();
    }

    private void DrawDirector() {
        var director = EventFramework.Instance()->GetInstanceContentOceanFishing();
        if (director == null) {
            ImGui.TextUnformatted("InstanceContentOceanFishing is null!");
            return;
        }

        var timeLeft = director->InstanceContentDirector.ContentDirector.ContentTimeLeft - director->TimeOffset;
        if (timeLeft < 0) timeLeft = 0;
        ImGui.TextUnformatted($"time left: {timeLeft}");

        ImGui.TextUnformatted($"spectral: {director->SpectralCurrentActive}");

        var route = this.ikdRoute.GetRow(director->CurrentRoute!)!;
        var spots = route.UnkData0.Select(x => this.ikdSpot.GetRow(x.Spot)!).ToList();
        var times = route.UnkData0.Select(x => x.Time).ToList();

        var spotsStr = string.Join(", ", spots.Select(x => x.PlaceName.Value!.Name.ToDalamudString().TextValue));
        ImGui.TextUnformatted($"route: {director->CurrentRoute} - {spotsStr}");


        var spot = spots[director->CurrentZone];
        var spotName = spot.PlaceName.Value!.Name.ToDalamudString().TextValue;
        var time = times[director->CurrentZone];
        var timeStr = time switch {
            1 => "Day",
            2 => "Sunset",
            3 => "Night",
            _ => "Unknown"
        };

        ImGui.TextUnformatted($"spot: {director->CurrentZone} -  {spotName} - {timeStr}");

        var missionOne = this.GetInfoForVoyageMission(director->Mission1Type);
        var missionTwo = this.GetInfoForVoyageMission(director->Mission2Type);
        var missionThree = this.GetInfoForVoyageMission(director->Mission3Type);
        ImGui.TextUnformatted($"{missionOne.Item2}: {director->Mission1Progress}/{missionOne.Item1}");
        ImGui.TextUnformatted($"{missionTwo.Item2}: {director->Mission2Progress}/{missionTwo.Item1}");
        ImGui.TextUnformatted($"{missionThree.Item2}: {director->Mission3Progress}/{missionThree.Item1}");
    }

    private void DrawFishingLog() {
        var agent = AgentModule.Instance()->GetAgentIKDFishingLog();
        if (agent == null) {
            ImGui.TextUnformatted("IKDFishingLog is null!");
            return;
        }

        ImGui.TextUnformatted($"points: {agent->Points}");
    }


    private void DrawResult() {
        var agent = AgentModule.Instance()->GetAgentIKDResult();
        if (agent == null) {
            ImGui.TextUnformatted("IKDResult is null!");
            return;
        }

        var result = agent->Data;
        if (result == null) {
            ImGui.TextUnformatted("Data is null!");
            return;
        }

        ImGui.TextUnformatted($"total points: {result->Score}");
    }

    private (byte, string) GetInfoForVoyageMission(uint rowId) {
        var row = this.ikdPlayerMissionCondition.GetRow(rowId)!;
        var amount = row.ReadColumn<byte>(0);
        var str = row.ReadColumn<SeString>(1)!.ToDalamudString().TextValue;
        return (amount, str);
    }

    private void DrawFishDataManager() {
        var spots = Plugin.FishData.Spots;
        var fish = spots
                   .Select(spot => spot.Fish)
                   .SelectMany(fish => fish)
                   // unique based off id
                   .GroupBy(fish => fish.ItemId)
                   .Select(group => group.First())
                   .ToList();

        ImGui.TextUnformatted($"Spots: {spots.Count}");
        ImGui.TextUnformatted($"Fish: {fish.Count}");

        foreach (var fishy in fish) {
            ImGui.Separator();

            var fishItem = this.item.GetRow(fishy.ItemId)!;
            var icon = Plugin.TextureProvider.GetIcon(fishItem.Icon)!;
            var lineHeight = ImGui.GetTextLineHeight();
            var iconSize = new Vector2(lineHeight, lineHeight);

            using (ImRaii.PushId(fishy.ItemId.ToString())) {
                ImGui.Image(icon.ImGuiHandle, iconSize);
                ImGui.SameLine();
                ImGui.TextUnformatted(fishItem.Name.ToDalamudString().TextValue);

                ImGui.TextUnformatted("Bite times:");
                using (ImRaii.PushIndent()) {
                    foreach (var biteTime in fishy.BiteTimes) {
                        var bait = this.item.GetRow(biteTime.Key)!;
                        var baitName = bait.Name.ToDalamudString().TextValue;
                        var baitIcon = Plugin.TextureProvider.GetIcon(bait.Icon)!;
                        var timeStr = Utils.FormatRange(biteTime.Value.Range);

                        ImGui.Image(baitIcon.ImGuiHandle, iconSize);
                        ImGui.SameLine();
                        ImGui.TextUnformatted($"{baitName}: {timeStr}");
                    }
                }

                ImGui.TextUnformatted("Average points: " + fishy.AveragePoints);
                ImGui.TextUnformatted("DH yield: " + Utils.FormatRange(fishy.DoubleHook));
                ImGui.TextUnformatted("TH yield: " + Utils.FormatRange(fishy.TripleHook));

                var bitePowerStr = string.Join("", Enumerable.Repeat("!", fishy.BitePower));
                ImGui.TextUnformatted("Bite: " + bitePowerStr);

                ImGui.TextUnformatted("Hookset: " + fishy.Hookset);

                if (fishy.Intuition != null) {
                    ImGui.TextUnformatted($"Intuition ({fishy.Intuition.Duration}):");
                    using (ImRaii.PushIndent()) {
                        foreach (var (id, amount) in fishy.Intuition.Fish) {
                            var fishRow = this.item.GetRow(id)!;
                            var fishName = fishRow.Name.ToDalamudString().TextValue;
                            var fishIcon = Plugin.TextureProvider.GetIcon(fishRow.Icon)!;

                            ImGui.Image(fishIcon.ImGuiHandle, iconSize);
                            ImGui.SameLine();
                            ImGui.TextUnformatted($"x{amount} {fishName}");
                        }
                    }
                }

                var hasTime = fishy.TimeAvailability.Count > 0;
                var hasWeather = fishy.WeatherAvailability.Count > 0;

                var alwaysUp = !hasTime && !hasWeather;
                if (alwaysUp) {
                    ImGui.TextUnformatted("Availability: Always");
                } else {
                    ImGui.TextUnformatted("Availability:");
                    using (ImRaii.PushIndent()) {
                        // Mutually exclusive
                        if (hasTime) {
                            foreach (var timeEntry in fishy.TimeAvailability) {
                                if (timeEntry.Value) ImGui.TextUnformatted(timeEntry.Key.ToString());
                            }
                        } else {
                            foreach (var weatherEntry in fishy.WeatherAvailability) {
                                if (weatherEntry.Value) {
                                    var weatherRow = this.weather.GetRow((uint) weatherEntry.Key)!;
                                    var weatherName = weatherRow.Name.ToDalamudString().TextValue;
                                    var weatherIcon = Plugin.TextureProvider.GetIcon((uint) weatherRow.Icon)!;

                                    ImGui.Image(weatherIcon.ImGuiHandle, iconSize);
                                    ImGui.SameLine();
                                    ImGui.TextUnformatted(weatherName);
                                }
                            }
                        }
                    }
                }

                var starStr = string.Join("", Enumerable.Repeat("*", fishy.Stars));
                ImGui.TextUnformatted("Stars: " + starStr);
            }
        }
    }

    private void DrawMisc() {
        if (ImGui.Button("Export lang")) {
            Plugin.LocalizationManager.Export();
        }
    }

    private void DrawPatterns() {
        var indigo = Plugin.FishData.Patterns[RouteType.Indigo];
        var ruby = Plugin.FishData.Patterns[RouteType.Ruby];

        for (var i = 0; i < indigo.Count; i++) {
            var schedule = indigo[i];
            ImGui.TextUnformatted($"{i}: {schedule.Destination} - {schedule.Time} - {schedule.Date}");
        }

        ImGui.Separator();

        for (var i = 0; i < ruby.Count; i++) {
            var schedule = ruby[i];
            ImGui.TextUnformatted($"{i}: {schedule.Destination} - {schedule.Time} - {schedule.Date}");
        }
    }

    private void DrawStateTracker() {
        ImGui.Checkbox("Use debug state tracker", ref Plugin.Configuration.UseDebugStateTracker);

        if (Plugin.Configuration.UseDebugStateTracker) {
            var dst = Plugin.DebugStateTracker;

            if (ImGui.Button("Dispatch enter")) Plugin.DispatchEnterExit(true);
            ImGui.SameLine();
            if (ImGui.Button("Dispatch exit")) Plugin.DispatchEnterExit(false);

            var isInOceanFishing = dst.IsInOceanFishing;
            if (ImGui.Checkbox("In ocean fishing", ref isInOceanFishing)) {
                dst.IsInOceanFishing = isInOceanFishing;
            }

            var isDataLoaded = dst.IsDataLoaded;
            if (ImGui.Checkbox("Data loaded", ref isDataLoaded)) {
                dst.IsDataLoaded = isDataLoaded;
            }

            var isInSpectral = dst.IsSpectralActive;
            if (ImGui.Checkbox("In spectral", ref isInSpectral)) {
                dst.IsSpectralActive = isInSpectral;
            }

            var timeLeft = dst.TimeLeftInZone;
            if (ImGui.SliderFloat("Time left in zone", ref timeLeft, 0, 60 * 7)) {
                dst.TimeLeftInZone = timeLeft;
            }

            var route = (int) dst.CurrentRoute;
            if (ImGui.InputInt("Current route", ref route)) {
                if (route < 1) route = 1;
                if (route > 18) route = 18;
                dst.CurrentRoute = (uint) route;
            }

            var zone = (int) dst.CurrentZone;
            if (ImGui.InputInt("Current zone", ref zone)) {
                if (zone < 0) zone = 0;
                if (zone > 2) zone = 2;
                dst.CurrentZone = (byte) zone;
            }

            var pts = (int) dst.Points;
            if (ImGui.InputInt("Points", ref pts)) {
                if (pts < 0) pts = 0;
                dst.Points = (ushort) pts;
            }

            var weathers = Enum.GetValues<WeatherType>();
            var currentWeather = (int) dst.CurrentWeather;
            if (ImGui.Combo(
                    "Current weather",
                    ref currentWeather,
                    weathers.Select(w => w.ToString()).ToArray(),
                    weathers.Length
                )) {
                dst.CurrentWeather = (WeatherType) currentWeather;
            }

            ImGui.Separator();
            var states = dst.MissionState;
            for (var i = 0; i < states.Count; i++) {
                this.DrawMissionControl(ref states, i);
            }
        }
    }

    private void DrawMissionControl(ref List<MissionState> states, int idx) {
        var state = states[idx];

        using (ImRaii.PushId(state.GetHashCode())) {
            ImGui.TextUnformatted(state.Objective);

            var row = (int) state.Row;
            if (ImGui.InputInt("Row", ref row)) {
                if (row < 0) row = 0;
                if (row > 34) row = 34;

                var newState = new MissionState((uint) row) {
                    Progress = state.Progress
                };
                states[idx] = newState;
            }

            var progress = (int) state.Progress;
            if (ImGui.InputInt("Progress", ref progress)) {
                if (progress < 0) progress = 0;
                if (progress > 100) progress = 100;
                states[idx].Progress = (byte) progress;
            }
        }

        ImGui.Separator();
    }

    private void DrawJournal() {
        ImGui.TextUnformatted("Is logging: " + Plugin.Journal.IsLogging);
        ImGui.TextUnformatted("Journal entries: " + Plugin.Journal.EntryCount);

        if (ImGui.Button("Save to file")) {
            Plugin.Journal.SaveToFile();
        }
    }
}
