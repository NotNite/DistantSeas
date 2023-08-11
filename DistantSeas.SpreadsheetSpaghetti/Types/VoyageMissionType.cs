using System.Text.Json.Serialization;

namespace DistantSeas.SpreadsheetSpaghetti.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VoyageMissionType {
    None,
    Shark,
    Jellyfish,
    Crab,
    Fugu,
    Squid,
    Shrimp,
    Shellfish
}
