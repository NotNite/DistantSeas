using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using DistantSeas.Core;
using ImGuiNET;

namespace DistantSeas.Windows;

public class MainWindow : DistantSeasWindow {
    private List<MainWindowSection> sections;
    private MainWindowSection? currentSection;

    public MainWindow() : base("Distant Seas") {
        this.sections = Plugin.ResourceManager.DistantSeasAssembly
                        .GetTypes()
                        .Where(x => x.IsSubclassOf(typeof(MainWindowSection)))
                        .Select(type => (MainWindowSection) Activator.CreateInstance(type)!)
#if !DEBUG
                        .Where(x => x.Category != MainWindowCategory.Debug)
#endif
                        .ToList();
    }

    public override void Dispose() {
        this.sections.ForEach(x => x.Dispose());
    }

    public override void Draw() {
        this.DrawSidebar();

        ImGui.SameLine();
        if (this.currentSection != null) {
            using (ImRaii.Child("##DistantSeasSection", ImGui.GetContentRegionAvail())) {
                this.currentSection.Draw();
            }
        }
    }

    private void DrawSidebar() {
        var longestName = this.sections
                              .Select(x => x.Name)
                              .Aggregate((x, y) => x.Length > y.Length ? x : y);

        // need this to align the text
        float longestIcon;
        using (ImRaii.PushFont(UiBuilder.IconFont)) {
            longestIcon = this.sections
                              .Select(x => x.Icon.ToIconString())
                              .Select(x => ImGui.CalcTextSize(x).X)
                              .Aggregate((x, y) => x > y ? x : y);
        }

        var width = ImGui.CalcTextSize(longestName).X + (100 * ImGuiHelpers.GlobalScale);
        var size = ImGui.GetContentRegionAvail() with {X = width};

        // sort by category, then alphabetically
        var categories = this.sections
                             .GroupBy(section => section.Category)
                             .OrderBy(group => (int) group.Key)
                             .ToList();

        using (ImRaii.Child("##DistantSeasSidebar", size, true)) {
            for (var i = 0; i < categories.Count; i++) {
                var category = categories[i].OrderBy(section => section.Name);
                foreach (var item in category) {
                    var start = ImGui.GetCursorPos();
                    using (ImRaii.PushFont(UiBuilder.IconFont)) {
                        ImGui.TextUnformatted(item.Icon.ToIconString());
                    }

                    var spacing = ImGui.GetStyle().ItemSpacing.X;
                    ImGui.SetCursorPos(
                        new Vector2(
                            start.X + longestIcon + spacing,
                            // <MidoriKami> text is 1 px too low
                            start.Y - 1
                        )
                    );

                    if (ImGui.Selectable(item.Name, this.currentSection == item)) {
                        this.currentSection = item;
                    }
                }

                if (i < categories.Count - 1) ImGui.Separator();
            }
        }
    }
}
