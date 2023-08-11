using System;
using Dalamud.Configuration;
using Newtonsoft.Json;

namespace DistantSeas;

[Serializable]
public class Configuration : IPluginConfiguration {
    public int Version { get; set; } = 0;

    [JsonProperty] public string? LanguageOverride = null;

    [JsonProperty] public bool ShowOverlay = true;
    [JsonProperty] public bool LockOverlay = true;
    [JsonProperty] public bool HideVanillaOverlay = false;
    [JsonProperty] public bool ScrollFish = true;
    [JsonProperty] public bool SortFish = true;

    [JsonProperty] public bool DrawVoyageMissions = true;
    [JsonProperty] public bool HideFinishedMissions = false;

    [JsonProperty] public bool DrawFishNames = true;
    [JsonProperty] public bool DrawFishRanges = false;
    [JsonProperty] public bool DrawIntTimes = true;
    [JsonProperty] public bool DrawFishPoints = false;
    [JsonProperty] public bool DrawSpectralColors = true; // TODO

    [JsonProperty] public bool AlarmEnabled = false;
    [JsonProperty] public int AlarmMinutes = 5;
    [JsonProperty] public bool AlarmSoundEnabled = false;
    [JsonProperty] public int AlarmSound = 6;

    [JsonProperty] public bool JournalEnabled = true;

    [JsonProperty] public bool OceanFishingBaitsOnly = false;
    [JsonProperty] public bool PreferDynamicSuggestions = false;

    [JsonProperty] public bool UseDebugStateTracker = false;

    public void Save() {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
