using System.Text.Json.Serialization;

namespace DistantSeas.SpreadsheetSpaghetti.Types; 

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CellType {
    None,
    BestOrRequired,
    Usable,
    Moochable,
    Mooched,
    MoochOnly,
    Intuition
}
