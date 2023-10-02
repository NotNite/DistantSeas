using System.IO;
using CheapLoc;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using DistantSeas.Core;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;

namespace DistantSeas.Windows.Main;

public class AboutSection : MainWindowSection {
    private IDalamudTextureWrap icon;

    public AboutSection() : base(
        FontAwesomeIcon.InfoCircle,
        Loc.Localize("AboutSection", "About"),
        MainWindowCategory.None
    ) {
        var iconPath = Plugin.ResourceManager.GetDataPath("icon.png");
        this.icon = Plugin.TextureProvider.GetTextureFromFile(new FileInfo(iconPath))!;
    }

    public override void Dispose() {
        this.icon.Dispose();
    }

    public override void Draw() {
        var iconWidth = ImGui.GetContentRegionAvail().X / 4;
        ImGuiHelpers.CenterCursorFor((int) iconWidth);
        ImGui.Image(this.icon.ImGuiHandle, new Vector2(iconWidth, iconWidth));

        var headerStr = Loc.Localize("AboutSectionHeader", "Distant Seas, by NotNite");
        var versionStr = Loc.Localize("AboutSectionVersion", "Version {0}");
        var version = Plugin.ResourceManager.DistantSeasAssembly.GetName().Version!.ToString();

        ImGuiHelpers.CenteredText(headerStr);
        ImGuiHelpers.CenteredText(string.Format(versionStr, version));

        var githubButtonStr = Loc.Localize("AboutSectionButtonGitHub", "Open source code/issues");
        var donateButtonStr = Loc.Localize("AboutSectionButtonDonate", "Donate to the plugin developer");

        var githubButtonSize = ImGuiHelpers.GetButtonSize(githubButtonStr);
        var donateButtonSize = ImGuiHelpers.GetButtonSize(donateButtonStr);
        var buttonSize = Vector2.Max(githubButtonSize, donateButtonSize);

        var size = (buttonSize.X * 2) + ImGui.GetStyle().ItemSpacing.X;
        ImGuiHelpers.CenterCursorFor((int) size);

        this.LinkButton(githubButtonStr, buttonSize, "https://github.com/NotNite/DistantSeas");
        ImGui.SameLine();
        this.LinkButton(donateButtonStr, buttonSize, "https://notnite.com/givememoney");

        ImGui.NewLine();

        // Note to all translators: I'm sorry
        ImGui.TextUnformatted(
            Loc.Localize("AboutSectionDescriptionStart",
                         "Distant Seas would not be possible without these people:")
        );

        ImGui.BulletText(
            Loc.Localize("AboutSectionPohky",
                         "Pohky, for helping reverse engineer Ocean Fishing")
        );
        this.IconLinkButton("https://github.com/pohky");

        ImGui.BulletText(
            Loc.Localize("AboutSectionTyoto",
                         "Tyo'to Tayuun, for their Ocean Fishing spreadsheet")
        );
        this.IconLinkButton("https://docs.google.com/spreadsheets/d/1R0Nt8Ye7EAQtU8CXF1XRRj67iaFpUk1BXeDgt6abxsQ/edit");

        ImGui.BulletText(
            Loc.Localize("AboutSectionPillowfication",
                         "pillowfication, for their Ocean Fishing website")
        );
        this.IconLinkButton("https://ffxiv.pf-n.co/ocean-fishing");

        ImGui.BulletText(
            Loc.Localize("AboutSectionFishcord",
                         "Members of the Fisherman's Horizon Discord server, for their fishing wisdom")
        );
        this.IconLinkButton("https://discord.gg/fishcord");

        ImGui.BulletText(
            Loc.Localize("AboutSectionGoatPlace",
                         "Members of the XIVLauncher & Dalamud Discord server, for their development wisdom")
        );
        this.IconLinkButton("https://goat.place/");

        ImGui.NewLine();

        ImGui.TextUnformatted(
            Loc.Localize("AboutSectionTranslators",
                         "Special thanks to these lovely translators:")
        );

        var translators = new[] {
            "Kung",
            "Aly",
            "redchair",
            "leechcop"
        };
        foreach (var translator in translators) {
            ImGui.BulletText(translator);
        }
    }

    private void IconLinkButton(string link) {
        ImGui.SameLine();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Link)) {
            Util.OpenLink(link);
        }

        if (ImGui.IsItemHovered()) ImGui.SetTooltip(link);
    }

    private void LinkButton(string text, Vector2 size, string link) {
        if (ImGui.Button(text, size)) {
            Util.OpenLink(link);
        }

        if (ImGui.IsItemHovered()) ImGui.SetTooltip(link);
    }
}
