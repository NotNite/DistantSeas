using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Timers;
using CheapLoc;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Logging;
using Dalamud.Utility;
using DistantSeas.Common;
using DistantSeas.Core;
using DistantSeas.Tracking;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace DistantSeas.Windows;

public class OverlayWindow : DistantSeasWindow {
    private ExcelSheet<IKDRoute> ikdRouteSheet;
    private ExcelSheet<IKDSpot> ikdSpotSheet;
    private ExcelSheet<Item> itemSheet;
    private bool tinting;

    private Timer fishTimer;
    private Spot? spot;
    private List<Fish>? availableFish;
    private List<Fish>? voyageFish;

    public OverlayWindow() : base("##DistantSeasOverlay") {
        this.ikdRouteSheet = Plugin.DataManager.Excel.GetSheet<IKDRoute>()!;
        this.ikdSpotSheet = Plugin.DataManager.Excel.GetSheet<IKDSpot>()!;
        this.itemSheet = Plugin.DataManager.Excel.GetSheet<Item>()!;

        this.fishTimer = new Timer(1000);
        this.fishTimer.AutoReset = true;
        this.fishTimer.Elapsed += (_, _) => this.UpdateFish();
        this.fishTimer.Start();
    }

    public override void Dispose() {
        this.fishTimer.Dispose();
    }

    public override void PreDraw() {
        this.Flags = ImGuiWindowFlags.NoCollapse
                     | ImGuiWindowFlags.NoDocking
                     | ImGuiWindowFlags.AlwaysAutoResize
                     | ImGuiWindowFlags.NoFocusOnAppearing
                     | ImGuiWindowFlags.NoDecoration;

        if (Plugin.Configuration.LockOverlay) {
            this.Flags |= ImGuiWindowFlags.NoMove;
        }

        this.tinting = Plugin.Configuration.DrawSpectralColors && Plugin.StateTracker.IsSpectralActive;
        var currentBg = ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.WindowBg));
        var tint = new Vector4(1f, 1f, 5f, 1f);

        if (this.tinting) {
            var r = MathF.Min(currentBg.X * tint.X, 1);
            var g = MathF.Min(currentBg.Y * tint.Y, 1);
            var b = MathF.Min(currentBg.Z * tint.Z, 1);
            var a = MathF.Min(currentBg.W * tint.W, 1);

            var vec = new Vector4(r, g, b, a);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, vec);
        }
    }

    public override void PostDraw() {
        if (this.tinting) ImGui.PopStyleColor();
    }

    public override void PreOpenCheck() {
        this.IsOpen = this.ShouldDraw();
    }

    private bool ShouldDraw() {
        return Plugin.StateTracker.IsInOceanFishing
               && Plugin.StateTracker.IsDataLoaded
               && Plugin.Configuration.ShowOverlay
               && !Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent];
    }

    private void UpdateFish() {
        if (this.ShouldDraw()) {
            this.spot = Plugin.BaitManager.GetCurrentSpot();
            this.availableFish = Plugin.BaitManager.GetAvailableFish();

            var missionState = Plugin.StateTracker.MissionState;
            this.voyageFish = Plugin.FishData.FilterForVoyageMission(
                availableFish, missionState).ToList();
        }
    }

    public override void Draw() {
        try {
            this.DrawHeader();

            if (Plugin.Configuration.OnlyDrawHeader) {
                return;
            }

            ImGui.Separator();

            if (Plugin.Configuration.DrawVoyageMissions) {
                if (this.DrawVoyageMissions()) {
                    ImGui.Separator();
                }
            }

            if (Plugin.Configuration.ScrollFish) {
                var maxSize = ImGui.GetContentRegionAvail() with {Y = 300 * ImGuiHelpers.GlobalScale};
                using (ImRaii.Child("##DistantSeasScrollFish", maxSize)) {
                    this.DrawFish();
                }
            } else {
                this.DrawFish();
            }
        } catch (Exception e) {
            // Catch here to prevent Blue Leakage:tm:
            PluginLog.Error(e, "Error drawing overlay window");
        }
    }

    private void DrawHeader() {
        var tracker = Plugin.StateTracker;

        // Spectral time remaining
        if (tracker.IsSpectralActive) {
            var specTime = Plugin.SpectralTimer.timer;
            var specTimeHuman = $"{(int) specTime / 60}:{(int) specTime % 60:00}";
            if (specTime <= 0) specTimeHuman = "0:00";
            Utils.IconText(FontAwesomeIcon.Clock, specTimeHuman);
            Utils.VerticalSeparator();
        }

        // Time remaining
        var zoneTime = tracker.TimeLeftInZone;
        var timeStr = $"{(int) zoneTime / 60}:{(int) zoneTime % 60:00}";
        if (zoneTime < 0) timeStr = "0:00";
        Utils.IconText(FontAwesomeIcon.Clock, timeStr);
        Utils.VerticalSeparator();
        
        // Points
        ImGui.TextUnformatted($"{tracker.Points}pts");
        Utils.VerticalSeparator();

        var isOverriden = Plugin.BaitManager.OverrideZone.HasValue;
        var overrideColor = ImGuiColors.ParsedBlue;
        var textColor = ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.Text));
        var toUseColor = isOverriden ? overrideColor : textColor;

        // Time of day and zone
        var currentRoute = this.ikdRouteSheet.GetRow(tracker.CurrentRoute)!;
        var currentZoneInfo = currentRoute.UnkData0[Plugin.BaitManager.CurrentZone];
        var currentZone = this.ikdSpotSheet.GetRow(currentZoneInfo.Spot)!;

        var zoneSpot = Plugin.StateTracker.IsSpectralActive
                           ? currentZone.SpotSub.Value!
                           : currentZone.SpotMain.Value!;
        var zoneName = zoneSpot.PlaceName.Value!.Name.ToDalamudString().TextValue;

        var time = (Time) currentZoneInfo.Time;
        var timeIcon = time switch {
            Time.Day => FontAwesomeIcon.Sun,
            Time.Sunset => FontAwesomeIcon.CloudSun,
            Time.Night => FontAwesomeIcon.Moon,
            _ => FontAwesomeIcon.Question
        };

        var zoneText = Loc.Localize("ZoneText", "Zone {0}");
        var zoneHoverText = Loc.Localize("ZoneHoverText",
                                         "Click to preview other zones. Weather information (and therefore suggested fish) may not be accurate.");

        // Time of day
        using (ImRaii.PushColor(ImGuiCol.Text, toUseColor)) {
            Utils.Icon(timeIcon);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Utils.TimeName(time));
            ImGui.SameLine();

            // Zone
            ImGui.TextUnformatted(zoneName);
            Utils.VerticalSeparator();

            // Zone number
            ImGui.TextUnformatted(string.Format(zoneText, Plugin.BaitManager.CurrentZone + 1));
        }

        if (ImGui.IsItemHovered()) ImGui.SetTooltip(zoneHoverText);
        if (ImGui.IsItemClicked()) ImGui.OpenPopup("ZoneSelector");

        using (var popup = ImRaii.ContextPopup("ZoneSelector")) {
            if (popup.Success) {
                for (var i = 0; i < 3; i++) {
                    var selected = Plugin.BaitManager.OverrideZone != null
                                       ? Plugin.BaitManager.OverrideZone == i
                                       : tracker.CurrentZone == i;

                    if (ImGui.Selectable(string.Format(zoneText, i + 1), selected)) {
                        Plugin.BaitManager.OverrideZone = (uint) i;
                        this.UpdateFish();
                    }
                }
            }
        }

        Utils.VerticalSeparator();

        // Current bait
        var currentBait = Plugin.BaitManager.CurrentBait;
        if (currentBait != 0) {
            var baitItem = this.itemSheet.GetRow(currentBait)!;
            var baitIcon = Plugin.TextureProvider.GetIcon(baitItem.Icon)!;

            var lineHeight = ImGui.GetTextLineHeight();
            var imageHeight = new Vector2(lineHeight, lineHeight);

            ImGui.Image(baitIcon.ImGuiHandle, imageHeight);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(baitItem.Name.ToDalamudString().TextValue);
        }
    }

    private bool DrawVoyageMissions() {
        var state = Plugin.StateTracker;
        var drewOne = false;

        foreach (var mission in state.MissionState) {
            var done = mission.Progress >= mission.Total;
            if (done && Plugin.Configuration.HideFinishedMissions) continue;

            ImGui.TextUnformatted($"{mission.Objective}: {mission.Progress}/{mission.Total}");
            drewOne = true;
        }

        return drewOne;
    }

    private void DrawFish() {
        if (this.spot == null || this.availableFish == null || this.voyageFish == null) {
            this.UpdateFish();
        }

        if (Plugin.Configuration.SortFish) {
            var spectral = this.availableFish!
                               .Where(x => x.CanCauseSpectral)
                               .ToList();
            var intuition = this.availableFish!
                                .Where(x => x.Intuition != null)
                                .ToList();

            var spectralHeader = Loc.Localize("OverlayWindowSpectralHeader", "Spectral");
            var intuitionHeader = Loc.Localize("OverlayWindowIntuitionHeader", "Intuition");
            var voyageHeader = Loc.Localize("OverlayWindowVoyageHeader", "Voyage");
            var normalHeader = Loc.Localize("OverlayWindowNormalHeader", "All");

            using (ImRaii.TabBar("##DistantSeasOverlaySort")) {
                if (spectral.Any()) {
                    using var tab = ImRaii.TabItem(spectralHeader);
                    if (tab.Success) this.DrawFishArray(this.spot!, spectral);
                }

                if (intuition.Any()) {
                    using var tab = ImRaii.TabItem(intuitionHeader);
                    if (tab.Success) this.DrawFishArray(this.spot!, intuition);
                }

                if (this.voyageFish!.Any()) {
                    using var tab = ImRaii.TabItem(voyageHeader);
                    if (tab.Success) this.DrawFishArray(this.spot!, this.voyageFish!);
                }

                using (var tab = ImRaii.TabItem(normalHeader)) {
                    if (tab.Success) this.DrawFishArray(this.spot!, this.availableFish!);
                }
            }
        } else {
            this.DrawFishArray(this.spot!, this.availableFish!);
        }
    }

    private void DrawFishArray(Spot spot, List<Fish> fishies) {
        var sorted = fishies.OrderByDescending(x => x.GetMaxPoints().Item3);
        foreach (var fish in sorted) this.DrawSpecificFish(spot, fish);
    }

    private void DrawSpecificFish(Spot spot, Fish fish) {
        var fishItem = this.itemSheet.GetRow(fish.ItemId)!;
        var fishName = fishItem.Name.ToDalamudString().TextValue;

        if (fish.Intuition != null) {
            this.DrawBaitChain(spot, fish, true, fishName);
            var durationStr = $"{fish.Intuition.Duration}s";
            var size = ImGui.GetTextLineHeight() * 2f;

            if (Plugin.Configuration.DrawIntTimes) {
                ImGui.SameLine();

                using (ImRaii.Group()) {
                    PrepareCenter(size);
                    Utils.Icon(FontAwesomeIcon.Clock);
                    ImGui.SameLine();

                    PrepareCenter(size);
                    ImGui.TextUnformatted(durationStr);
                }

                if (ImGui.IsItemHovered()) {
                    var intTime = Loc.Localize(
                        "OverlayIntTime",
                        "This fish requires intuition to catch. The intuition effect lasts {0} seconds."
                    );
                    ImGui.SetTooltip(string.Format(intTime, fish.Intuition.Duration));
                }
            }

            using (ImRaii.PushIndent()) {
                foreach (var (id, amount) in fish.Intuition.Fish) {
                    var intFish = spot.Fish.First(x => x.ItemId == id);
                    var intFishItem = this.itemSheet.GetRow(intFish.ItemId)!;
                    var intFishName = intFishItem.Name.ToDalamudString().TextValue;
                    var text = $"{amount}x {intFishName}";

                    this.DrawBaitChain(spot, intFish, false, text);
                }
            }
        } else {
            // Normal
            this.DrawBaitChain(spot, fish, true, fishName);
        }
    }

    private void DrawBaitChain(Spot spot, Fish fish, bool big, string name) {
        var baitChain = Plugin.BaitManager.GetBaitChain(fish.ItemId);
        var lineHeight = ImGui.GetTextLineHeight();
        var multiplier = big ? 2 : 1.5f;
        var iconSize = new Vector2(lineHeight * multiplier, lineHeight * multiplier);

        for (var i = 0; i < baitChain.Count; i++) {
            var itemId = baitChain[i];
            var item = this.itemSheet.GetRow(itemId)!;
            var icon = Plugin.TextureProvider.GetIcon(item.Icon);

            var canSwitchBait = Utils.IsBait(itemId)
                                && Plugin.BaitManager.CanChangeBait()
                                && Plugin.BaitManager.CurrentBait != itemId;

            var biteTimeStr = Loc.Localize("OverlayTooltipBiteTime", "Bite time: {0}");
            var bitePowerStr = Loc.Localize("OverlayTooltipBitePower", "Bite power: {0}");
            var starsStr = Loc.Localize("OverlayTooltipStars", "Stars: {0}");
            var voyageMissionTypeStr = Loc.Localize("OverlayTooltipVoyageMissionType", "Voyage mission type: {0}");
            var baitSwitchStr = Loc.Localize("OverlayTooltipBaitSwitch", "Click to switch to this bait.");

            var drawingFishRange = Plugin.Configuration.DrawFishRanges && i != 0;
            var bitePower = Utils.GetBitePowerStr(spot, itemId);
            var biteTime = i != 0
                               ? Utils.GetBiteTimeStr(spot, itemId, baitChain[i - 1])
                               : null;

            var cursor = ImGui.GetCursorPos();
            ImGui.Image(icon.ImGuiHandle, iconSize);

            if (ImGui.IsItemHovered()) {
                var str = item.Name.ToDalamudString().TextValue;
                str += "\n" + string.Format(starsStr, Utils.GetStarStr(fish));

                if (fish.VoyageMissionType != VoyageMissionType.None) {
                    var typeName = Utils.VoyageMissionTypeName(fish.VoyageMissionType);
                    str += "\n" + string.Format(voyageMissionTypeStr, typeName);
                }

                if (biteTime != null) str += "\n" + string.Format(biteTimeStr, biteTime);
                if (bitePower != null) str += "\n" + string.Format(bitePowerStr, bitePower);
                if (canSwitchBait) str += $"\n{baitSwitchStr}";
                ImGui.SetTooltip(str);
            }

            if (ImGui.IsItemClicked() && canSwitchBait) {
                Plugin.BaitManager.SetCurrentBait(itemId);
            }

            var drawingArrowRight = i != baitChain.Count - 1;
            var shouldSameLine = drawingArrowRight
                                 || Plugin.Configuration.DrawFishNames
                                 || Plugin.Configuration.DrawFishPoints;

            if (drawingFishRange) {
                var rangeCursor = cursor with {Y = cursor.Y + iconSize.Y + ImGui.GetStyle().ItemSpacing.Y};

                if (biteTime != null) {
                    var size = ImGui.CalcTextSize(biteTime).X;
                    rangeCursor.X += (iconSize.X - size) / 2;

                    // Draw the range
                    ImGui.SameLine();
                    var retCursor = ImGui.GetCursorPos();
                    ImGui.SetCursorPos(rangeCursor);

                    var str = $"{biteTime} ({bitePower})";
                    ImGui.TextUnformatted(str);

                    // Get back to the normal spot
                    ImGui.SameLine();
                    ImGui.SetCursorPos(retCursor);

                    if (!shouldSameLine) {
                        // Fake an element to appease the ImGui cursor gods
                        // (why? see for yourself: comment it out, hide names, show bite times)
                        ImGui.TextUnformatted("");
                    }
                }
            } else {
                if (shouldSameLine) ImGui.SameLine();
            }

            if (drawingArrowRight) {
                PrepareCenter(iconSize.Y);
                Utils.Icon(FontAwesomeIcon.ArrowRight);
                ImGui.SameLine();
            }
        }

        if (Plugin.Configuration.DrawFishNames) {
            PrepareCenter(iconSize.Y);
            ImGui.TextUnformatted(name);
            if (Plugin.Configuration.DrawFishPoints) ImGui.SameLine();
        }

        var points = fish.GetMaxPoints();
        if (Plugin.Configuration.DrawFishPoints && points.Item3 > 0) {
            var yield = points.Item2;
            var pts = points.Item3;
            var str = points.Item1 switch {
                HookType.Double => $"DH x{yield}: {pts}",
                HookType.Triple => $"TH x{yield}: {pts}",
                _ => yield.ToString()
            };

            ImGui.SameLine();
            using (ImRaii.Group()) {
                PrepareCenter(iconSize.Y);
                Utils.Icon(FontAwesomeIcon.Star);
                ImGui.SameLine();

                PrepareCenter(iconSize.Y);
                ImGui.TextUnformatted(str);
            }
        }
    }

    private void PrepareCenter(float size) {
        var cursorPos = ImGui.GetCursorPosY();
        var lineHeight = ImGui.GetTextLineHeight();
        ImGui.SetCursorPosY(cursorPos + (size - lineHeight) / 2);
    }
}
