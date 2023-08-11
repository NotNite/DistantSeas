using System.Text.Json.Serialization;

namespace DistantSeas.SpreadsheetSpaghetti.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Hookset {
    Precision,
    Powerful
}
