using System.Text.Json.Serialization;

namespace DistantSeas.Common;

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
