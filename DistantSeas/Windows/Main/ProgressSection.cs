using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CheapLoc;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using DistantSeas.Core;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace DistantSeas.Windows.Main;

public class ProgressSection : MainWindowSection {
    private const uint Goal = 3_000_000;
    private ExcelSheet<Achievement> achievementSheet;

    public ProgressSection() : base(
        FontAwesomeIcon.BarsProgress,
        Loc.Localize("ProgressSection", "Progress"),
        MainWindowCategory.Tracking
    ) {
        this.achievementSheet = Plugin.DataManager.GetExcelSheet<Achievement>()!;
    }

    public override void Draw() {
        var width = ImGui.GetContentRegionAvail().X;
        var state = Plugin.AchievementTracker.GetState();

        using (ImRaii.PushFont(Plugin.HeaderFontHandle.ImFont)) {
            var text = state.TotalPoints.ToString("N0");
            ImGuiHelpers.CenteredText(text);
        }

        var progressBarWidth = width / 2;
        var progressBarStart = (width / 2) - (progressBarWidth / 2);
        var progress = state.TotalPoints / (float) Goal;

        ImGui.SetCursorPosX(progressBarStart);
        ImGui.ProgressBar(progress, new Vector2(progressBarWidth, 0));

        var str = Loc.Localize("ProgressSectionHeadsUp",
                               "Looks wrong? Open the Achievements menu to sync completion state, and select On a Boat V to sync total points.");
        ImGuiHelpers.CenteredText(str);

        var achievements = Plugin.AchievementTracker.Achievements
                                 .Select(x => this.achievementSheet.GetRow(x)!)
                                 .OrderBy(x => x.Name.ToDalamudString().TextValue)
                                 .ToList();

        var flags = ImGuiTableFlags.Borders
                    | ImGuiTableFlags.SizingFixedFit
                    | ImGuiTableFlags.ScrollY
                    | ImGuiTableFlags.RowBg;


        using (ImRaii.Table("##DistantSeasProgress", 4, flags)) {
            ImGui.TableSetupColumn(Loc.Localize("ProgressSectionCompleted", "Completed"));
            ImGui.TableSetupColumn(Loc.Localize("ProgressSectionIcon", "Icon"));
            ImGui.TableSetupColumn(Loc.Localize("ProgressSectionName", "Name"));
            ImGui.TableSetupColumn(Loc.Localize("ProgressSectionDescription", "Description"));
            ImGui.TableHeadersRow();

            foreach (var achievement in achievements) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                var completed = state.CompletedAchievements.GetValueOrDefault(achievement.RowId);

                if (completed) {
                    this.DrawAchievement(achievement, completed);
                } else {
                    using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled))) {
                        this.DrawAchievement(achievement, completed);
                    }
                }
            }
        }
    }

    private void DrawAchievement(Achievement achievement, bool completed) {
        var completedIcon = completed
                                ? FontAwesomeIcon.Check
                                : FontAwesomeIcon.Times;

        Utils.Icon(completedIcon);
        ImGui.TableNextColumn();

        var iconId = achievement.Icon;
        var icon = Plugin.TextureProvider.GetIcon(iconId)!;
        var iconSize = ImGui.GetTextLineHeight();
        ImGui.Image(icon.ImGuiHandle, new Vector2(iconSize, iconSize));
        ImGui.TableNextColumn();

        ImGui.TextUnformatted(achievement.Name);
        ImGui.TableNextColumn();

        ImGui.TextUnformatted(achievement.Description);
    }
}
