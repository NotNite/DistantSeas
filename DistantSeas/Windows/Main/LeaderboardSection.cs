using CheapLoc;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using DistantSeas.Core;
using ImGuiNET;

namespace DistantSeas.Windows.Main;

public class LeaderboardSection : MainWindowSection {
    public LeaderboardSection() : base(
        FontAwesomeIcon.Trophy,
        Loc.Localize("LeaderboardSection", "Leaderboard"),
        MainWindowCategory.Tracking
    ) { }

    public override void Draw() {
        using (ImRaii.PushFont(Plugin.HeaderFontHandle.ImFont)) {
            ImGui.TextUnformatted(Loc.Localize("LeaderboardSectionSoon", "soon:tm:"));
        }

        ImGui.TextUnformatted(
            Loc.Localize("LeaderboardSectionSoon2", "The leaderboard is a work in progress - stay tuned!")
        );
    }
}
