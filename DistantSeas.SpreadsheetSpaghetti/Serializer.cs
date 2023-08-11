using System.Text.Json;

namespace DistantSeas.SpreadsheetSpaghetti;

public static class Serializer {
    private static JsonSerializerOptions Options = new() {
        IncludeFields = true
    };

    public static string ToString(object obj) {
        return JsonSerializer.Serialize(obj, Options);
    }

    public static T FromString<T>(string json) {
        return JsonSerializer.Deserialize<T>(json, Options)!;
    }
}
