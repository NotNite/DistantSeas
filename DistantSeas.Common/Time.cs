using System.Text.Json.Serialization;

namespace DistantSeas.Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Time {
    Unknown = 0,
    Day = 1,
    Sunset = 2,
    Night = 3
}
