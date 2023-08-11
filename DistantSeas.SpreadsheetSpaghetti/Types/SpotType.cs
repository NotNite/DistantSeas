using System.Text.Json.Serialization;

namespace DistantSeas.SpreadsheetSpaghetti.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpotType {
    Unknown = 0,
    GaladionBay = 1,
    SouthernMerlthor = 2,
    NorthernMerlthor = 3,
    RhotanoSea = 4,
    CieldalaesMargin = 5,
    BloodbrineSea = 6,
    RothlytSound = 7,

    SirensongSea = 8,
    KuganeCoast = 9,
    RubySea = 10,
    OneRiver = 11
}
