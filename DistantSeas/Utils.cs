using System.Linq;
using System.Numerics;
using CheapLoc;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using DistantSeas.Common;
using Range = DistantSeas.Common.Range;

namespace DistantSeas;

public static class Utils {
    public const uint Ragworm = 29714;
    public const uint Krill = 29715;
    public const uint PlumpWorm = 29716;

    public static string SpotTypeName(SpotType type) {
        var rowId = (uint) type;
        var sheet = Plugin.DataManager.Excel.GetSheet<IKDSpot>()!;
        var row = sheet.GetRow(rowId)!;
        return row.PlaceName.Value!.Name.ToDalamudString().TextValue;
    }

    public static string TimeName(Time time) {
        return time switch {
            Time.Day => Loc.Localize("TimeDay", "Day"),
            Time.Sunset => Loc.Localize("TimeSunset", "Sunset"),
            Time.Night => Loc.Localize("TimeNight", "Night"),
            _ => "???"
        };
    }

    public static string VoyageMissionTypeName(VoyageMissionType type) {
        return type switch {
            VoyageMissionType.Shark => Loc.Localize("VoyageMissionTypeShark", "Shark"),
            VoyageMissionType.Jellyfish => Loc.Localize("VoyageMissionTypeJellyfish", "Jellyfish"),
            VoyageMissionType.Crab => Loc.Localize("VoyageMissionTypeCrab", "Crab"),
            VoyageMissionType.Fugu => Loc.Localize("VoyageMissionTypeFugu", "Fugu"),
            VoyageMissionType.Squid => Loc.Localize("VoyageMissionTypeSquid", "Squid"),
            VoyageMissionType.Shrimp => Loc.Localize("VoyageMissionTypeShrimp", "Shrimp"),
            VoyageMissionType.Shellfish => Loc.Localize("VoyageMissionTypeShellfish", "Shellfish"),
            _ => "???"
        };
    }

    public static Vector2 CalcIconSize(FontAwesomeIcon icon) {
        using (ImRaii.PushFont(UiBuilder.IconFont)) {
            return ImGui.CalcTextSize(icon.ToIconString());
        }
    }

    public static void IconText(FontAwesomeIcon icon, string text) {
        Icon(icon);
        ImGui.SameLine();
        ImGui.TextUnformatted(text);
    }

    public static void Icon(FontAwesomeIcon icon) {
        using (ImRaii.PushFont(UiBuilder.IconFont)) {
            ImGui.TextUnformatted(icon.ToIconString());
        }
    }

    public static void VerticalSeparator() {
        ImGui.SameLine();

        var cursorX = ImGui.GetCursorPosX();
        var cursorY = ImGui.GetCursorPosY();

        var y1 = cursorY;
        var y2 = y1 + ImGui.GetTextLineHeight();
        var windowPos = ImGui.GetWindowPos();
        var b1 = new Vector2(cursorX, y1) + windowPos;
        var b2 = new Vector2(cursorX + 1, y2) + windowPos;

        ImGui.GetWindowDrawList().AddRectFilled(b1, b2, ImGui.GetColorU32(ImGuiCol.Separator));

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        ImGui.SetCursorPos(new Vector2(cursorX + 1 + spacing, cursorY));
    }

    public static string FormatRange(Range? range) {
        if (range == null) return "???";
        return range.Type switch {
            Range.RangeType.Range => $"{range.Start}-{range.End}s",
            Range.RangeType.LooseRange => $"{range.Start}s+",
            Range.RangeType.Single => $"{range.Start}s",
            _ => "???"
        };
    }

    public static bool IsBait(uint id) {
        return Plugin.FishData.Baits.Contains(id) && !IsFish(id);
    }

    public static bool IsFish(uint id) {
        return Plugin.FishData.Fish.Any(x => x.ItemId == id);
    }

    public static string? GetBiteTimeStr(Spot spot, uint id, uint bait) {
        var fish = spot.Fish.FirstOrDefault(x => x.ItemId == id);
        if (fish == null) return null;

        var range = fish.BiteTimes.TryGetValue(bait, out var time) ? time.Range : fish.Mooch?.Range;
        return range == null ? null : FormatRange(range);
    }

    public static string? GetBitePowerStr(Spot spot, uint id) {
        var fish = spot.Fish.FirstOrDefault(x => x.ItemId == id);
        if (fish == null) return null;
        return string.Concat(Enumerable.Repeat("!", fish.BitePower));
    }

    public static string GetStarStr(Fish fish) {
        const string star = "★";
        return string.Concat(Enumerable.Repeat(star, fish.Stars));
    }

    public static bool DisabledButtonWithTooltip(
        bool disabled,
        string label,
        string enabledText = "",
        string disabledText = ""
    ) {
        if (disabled) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
        var ret = ImGui.Button(label);
        if (disabled) ImGui.PopStyleVar();

        var str = disabled ? disabledText : enabledText;
        if (!string.IsNullOrEmpty(str) && ImGui.IsItemHovered()) ImGui.SetTooltip(str);

        return ret && !disabled;
    }
}
