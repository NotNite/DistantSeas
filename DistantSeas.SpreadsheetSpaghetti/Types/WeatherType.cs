using System.Text.Json.Serialization;

namespace DistantSeas.SpreadsheetSpaghetti.Types; 

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WeatherType : uint {
    Unknown = 0,
    ClearSkies = 1,
    FairSkies = 2,
    Clouds = 3,
    Fog = 4,
    Wind = 5,
    Gales = 6,
    Rain = 7,
    Showers = 8,
    Thunder = 9,
    Thunderstorms = 10,
    DustStorms = 11,
    HeatWaves = 14,
    Snow = 15,
    Blizzards = 16,
    
    SpectralCurrent = 145
}
