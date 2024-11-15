using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CheapLoc;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using DistantSeas.Common;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace DistantSeas.Fishing;

public class FishFilter {
    private readonly FishRaii raii;
    
    private string search = string.Empty;
    
    private readonly List<Item> baits = new();
    private readonly List<VoyageMissionType> missions = new();

    public FishFilter(FishRaii raii) {
        this.raii = raii;
        this.raii.Updated += this.Update;
    }

    private void Update() {
        var bait = this.raii.GetUsableBait();
        this.baits.RemoveAll(item => !bait.Contains(item));
    }

    public IEnumerable<Fish> Apply(IEnumerable<Fish> fishes) {
        if (!this.search.IsNullOrEmpty()) {
            fishes = fishes.Where(fish => {
                var name = this.raii.GetFishItem(fish).Name.ToDalamudString().TextValue;
                return name.Contains(this.search, StringComparison.InvariantCultureIgnoreCase);
            });
        }
        if (this.baits.Count > 0) {
            fishes = fishes.Where(fish => {
                var baitId = Plugin.BaitManager.GetBaitChain(fish.ItemId).First();
                return this.baits.Any(bait => bait.RowId == baitId);
            });
        }
        if (this.missions.Count > 0) {
            fishes = fishes.Where(fish => this.missions.Contains(fish.VoyageMissionType));
        }
        return fishes;
    }

    private const string PopupId = "##DSFishFilterPopup";

    public void Draw() {
        ImGui.InputTextWithHint(
            "##DSFishFilterSearch",
            Loc.Localize("OverlayWindowSearch", "Search"),
            ref this.search,
            256
        );
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        if (ImGui.Button(Loc.Localize("OverlayWindowFilter", "Filter")))
            ImGui.OpenPopup(PopupId);
        this.DrawPopup();
    }

    private void DrawPopup() {
        using var popup = ImRaii.Popup(PopupId);
        if (!popup.Success) return;
        
        this.DrawBaitCombo();
        this.DrawMissionCombo();
    }

    private void DrawBaitCombo() {
        var preview = this.baits.Count > 0
                          ? string.Join(", ", this.baits.Select(bait => bait.Name))
                          : Loc.Localize("OverlayWindowFilterNone", "None");
        if (!ImGui.BeginCombo(Loc.Localize("OverlayWindowFilterBait", "Bait"), preview))
            return;
        
        try {
            var height = ImGui.GetTextLineHeight();
            var iconSize = new Vector2(height, height);
            
            var bait = this.raii.GetUsableBait();
            foreach (var item in bait) {
                var cursor = ImGui.GetCursorPos();
                Select($"##Bait{item.RowId}", this.baits, item);
                ImGui.SetCursorPos(cursor);

                var icon = Plugin.TextureProvider.GetFromGameIcon((int) item.Icon);
                ImGui.Image(icon.GetWrapOrEmpty().ImGuiHandle, iconSize);
                ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
                ImGui.Text(item.Name.ExtractText());
            }
        } finally {
            ImGui.EndCombo();
        }
    }

    private void DrawMissionCombo() {
        var preview = this.missions.Count > 0
                          ? string.Join(", ", this.missions)
                          : Loc.Localize("OverlayWindowFilterNone", "None");
        if (!ImGui.BeginCombo(Loc.Localize("OverlayWindowFilterMission", "Mission"), preview))
            return;

        try {
            var missionTypes = Enum.GetValues<VoyageMissionType>().Skip(1);
            foreach (var mission in missionTypes)
                Select(Utils.VoyageMissionTypeName(mission), this.missions, mission);
        } finally {
            ImGui.EndCombo();
        }
    }

    private static void Select<T>(string label, ICollection<T> list, T item) {
        var isSelected = list.Contains(item);
        if (ImGui.Selectable(label, isSelected, ImGuiSelectableFlags.DontClosePopups)) {
            if (isSelected)
                list.Remove(item);
            else
                list.Add(item);
        }
    }
}
