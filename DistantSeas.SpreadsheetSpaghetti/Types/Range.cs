using System.Text.Json.Serialization;

namespace DistantSeas.SpreadsheetSpaghetti.Types;

public class Range {
    public RangeType Type;
    public int Start;
    public int? End;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RangeType {
        Range,      // 7-13
        LooseRange, // 7+
        Single      // 7
    }
}
