using System.Collections.Generic;
using System.Linq;
using CheapLoc;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using DistantSeas.Core;
using ImGuiNET;

namespace DistantSeas.Windows.Main;

public class SettingsSection : MainWindowSection {
    public SettingsSection() : base(
        FontAwesomeIcon.Cog,
        Loc.Localize("SettingsSection", "Settings"),
        MainWindowCategory.None
    ) { }

    public override void Draw() {
        this.DrawLanguageSelector();
        ImGui.NewLine();

        ImGui.Checkbox(
            Loc.Localize("SettingsShowOverlay", "Show overlay"),
            ref Plugin.Configuration.ShowOverlay
        );
        ImGuiComponents.HelpMarker(
            Loc.Localize("SettingsShowOverlayDescription",
                         "When in Ocean Fishing, draws an overlay with information about your current boat.")
        );

        using (ImRaii.PushIndent()) {
            if (Plugin.Configuration.ShowOverlay) {
                this.DrawOverlaySettings();
            } else {
                using (ImRaii.Disabled()) {
                    this.DrawOverlaySettings();
                }
            }
        }

        ImGui.NewLine();

        ImGui.Checkbox(
            Loc.Localize("SettingsAlarmEnabled", "Enable alarm"),
            ref Plugin.Configuration.AlarmEnabled
        );
        ImGuiComponents.HelpMarker(
            Loc.Localize("SettingsAlarmEnabledDescription",
                         "Posts a message in chat (and optionally plays a sound) before the next boat.")
        );

        using (ImRaii.PushIndent()) {
            if (Plugin.Configuration.AlarmEnabled) {
                this.DrawAlarmSettings();
            } else {
                using (ImRaii.Disabled()) {
                    this.DrawAlarmSettings();
                }
            }
        }

        ImGui.NewLine();

        ImGui.Checkbox(
            Loc.Localize("SettingsJournalEnabled", "Enable journal"),
            ref Plugin.Configuration.JournalEnabled
        );
        ImGuiComponents.HelpMarker(
            Loc.Localize("SettingsJournalEnabledDescription",
                         "Saves a log of each boat you're on. Disabling this will not remove old logs.")
        );

        ImGui.NewLine();

        ImGui.Checkbox(
            Loc.Localize("SettingsOceanFishingBaitsOnly", "Prefer Ocean Fishing baits"),
            ref Plugin.Configuration.OceanFishingBaitsOnly
        );
        ImGuiComponents.HelpMarker(
            Loc.Localize("SettingsOceanFishingBaitsOnlyDescription",
                         "Prefers Ragworm, Krill, and Plump Worm as baits when possible.")
        );

        ImGui.Checkbox(
            Loc.Localize("SettingsPreferDynamicSuggestions", "Prefer dynamic suggestions"),
            ref Plugin.Configuration.PreferDynamicSuggestions
        );
        ImGuiComponents.HelpMarker(
            Loc.Localize("SettingsPreferDynamicSuggestionsDescription",
                         "Use bait with the least bite time overlap instead of community-defined baits. Will likely be less accurate.")
        );
    }

    private void DrawLanguageSelector() {
        var strs = new List<string> {
                Loc.Localize("SettingsUseDalamudLanguage", "Use Dalamud language")
            }.Concat(LocalizationManager.CodesToNames.Values)
             .ToList();

        var pos = Plugin.Configuration.LanguageOverride == null
                      ? 0
                      : strs.IndexOf(
                          LocalizationManager.CodesToNames[Plugin.Configuration.LanguageOverride]
                      );

        if (ImGui.Combo(
                Loc.Localize("SettingsLanguageOverride", "Language"),
                ref pos,
                strs.ToArray(),
                strs.Count
            )) {
            if (pos == 0) {
                Plugin.Configuration.LanguageOverride = null;
            } else {
                var key = LocalizationManager.CodesToNames.First(x => x.Value == strs[pos]).Key;
                Plugin.Configuration.LanguageOverride = key;
            }

            Plugin.LocalizationManager.Setup();
        }
    }

    private void DrawOverlaySettings() {
        ImGui.Checkbox(
            Loc.Localize("SettingsLockOverlay", "Lock overlay"),
            ref Plugin.Configuration.LockOverlay
        );

        ImGui.Checkbox(
            Loc.Localize("SettingsHideVanillaOverlay", "Hide original overlay"),
            ref Plugin.Configuration.HideVanillaOverlay
        );
        ImGuiComponents.HelpMarker(
            Loc.Localize("SettingsHideVanillaOverlayDescription",
                         "Hides the base game fishing log and voyage missions.")
        );

        ImGui.Checkbox(
            Loc.Localize("SettingsScrollFish", "Add scrollbar to fish"),
            ref Plugin.Configuration.ScrollFish
        );

        ImGui.Checkbox(
            Loc.Localize("SettingsSortFish", "Group fish by type"),
            ref Plugin.Configuration.SortFish
        );
        ImGuiComponents.HelpMarker(
            Loc.Localize("SettingsSortFishDescription",
                         "Groups fish into tabs.")
        );

        ImGui.Checkbox(
            Loc.Localize("SettingsDrawVoyageMissions", "Show voyage missions"),
            ref Plugin.Configuration.DrawVoyageMissions
        );

        ImGui.Checkbox(
            Loc.Localize("SettingsHideFinishedMissions", "Hide finished voyage missions"),
            ref Plugin.Configuration.HideFinishedMissions
        );

        ImGui.Checkbox(
            Loc.Localize("SettingsDrawFishNames", "Show fish names"),
            ref Plugin.Configuration.DrawFishNames
        );

        ImGui.Checkbox(
            Loc.Localize("SettingsDrawFishRanges", "Show fish bite times"),
            ref Plugin.Configuration.DrawFishRanges
        );

        ImGui.Checkbox(
            Loc.Localize("SettingsDrawIntTimes", "Show intuition durations"),
            ref Plugin.Configuration.DrawIntTimes
        );
        ImGuiComponents.HelpMarker(
            Loc.Localize("SettingsDrawIntTimesDescription",
                         "For fish that require intuition to be caught, shows the intuition's duration next to their icons.")
        );

        ImGui.Checkbox(
            Loc.Localize("SettingsDrawFishPoints", "Show average points of fish"),
            ref Plugin.Configuration.DrawFishPoints
        );
        ImGuiComponents.HelpMarker(
            Loc.Localize("SettingsDrawFishPointsDescription",
                         "Draws the maximum points that can be obtained from each fish. This is calculated by the maximum yield of fish multiplied by the average points.")
        );

        ImGui.Checkbox(
            Loc.Localize("SettingsDrawSpectralColors", "Change color in spectral currents"),
            ref Plugin.Configuration.DrawSpectralColors
        );
        ImGuiComponents.HelpMarker(
            Loc.Localize("SettingsDrawSpectralColorsDescription",
                         "Visually indicate you are in a spectral current by tinting the background color blue.")
        );
    }

    private void DrawAlarmSettings() {
        var width = 100 * ImGuiHelpers.GlobalScale;

        ImGui.PushItemWidth(width);
        ImGui.SliderInt(
            Loc.Localize("SettingsAlarmMinutes", "Minutes before to trigger"),
            ref Plugin.Configuration.AlarmMinutes,
            1,
            60
        );
        ImGui.PopItemWidth();

        ImGui.Checkbox(
            Loc.Localize("SettingsAlarmSoundEnabled", "Play sound"),
            ref Plugin.Configuration.AlarmSoundEnabled
        );

        ImGui.PushItemWidth(width);
        var sound = Plugin.Configuration.AlarmSound;
        var soundStr = Loc.Localize("SettingsAlarmSound", "Sound");
        if (ImGui.InputInt(soundStr, ref sound)) {
            if (sound < 1) sound = 1;
            if (sound > 16) sound = 16;
            Plugin.Configuration.AlarmSound = sound;
            Plugin.AlarmManager.ClearState();
        }
        ImGui.PopItemWidth();
        ImGuiComponents.HelpMarker(
            Loc.Localize("SettingsAlarmSoundDescription",
                         "The sound to play when the alarm triggers. This is the same as the <se.X> sounds in chat.")
        );
    }
}
