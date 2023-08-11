using System.Text.Json.Serialization;

namespace DistantSeas.SpreadsheetSpaghetti.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RouteType {
    Indigo,
    Ruby
}
