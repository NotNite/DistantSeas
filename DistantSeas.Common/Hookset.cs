using System.Text.Json.Serialization;

namespace DistantSeas.Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Hookset {
    Precision,
    Powerful
}
