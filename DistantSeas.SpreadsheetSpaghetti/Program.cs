using System.Text.RegularExpressions;
using DistantSeas.Common;
using DistantSeas.SpreadsheetSpaghetti;
using Lumina;
using Lumina.Excel.Sheets;
using Range = DistantSeas.Common.Range;

// not exhaustive because of an "unnamed enum value", what the fuck are you on
#pragma warning disable CS8524

// replace ID in spreadsheet URL if it ever moves
// GIDs are found by going to the tab and looking at the end of the URL
const string spreadsheetUrl =
    "https://docs.google.com/spreadsheets/d/1uuK0E6pfBtW0Jdlexy8gPbT1p5WTr_Cruv7mmgR3BH0/export?format=tsv&gid=";

var gamePath = args[0];
var outPath = args[1];

var lumina = new GameData(gamePath);
using var http = new HttpClient();

var ikdSpot = lumina.GetExcelSheet<IKDSpot>()!;
var item = lumina.GetExcelSheet<Item>()!;
var weather = lumina.GetExcelSheet<Weather>()!;

const string star = "★: ";

Spot ProcessSpot(
    RouteType type,
    IKDSpot spot,
    bool spectral,
    string[] tsvRows
) {
    var fish = new List<Fish>();

    var spotName = spectral
                       ? spot.SpotSub.Value!.PlaceName.Value!.Name.ExtractText()
                       : spot.SpotMain.Value!.PlaceName.Value!.Name.ExtractText();

    var fishRows = new List<string>();
    var collecting = false;
    var collectingHeader = false;

    for (var i = 0; i < tsvRows.Length; i++) {
        var row = tsvRows[i];

        // Fucking why
        if (row.Trim() == string.Empty) continue;

        if (collecting) {
            if (!collectingHeader) {
                var isIndent = row.StartsWith("\t");
                if (!isIndent) break;
            }

            fishRows.Add(row.TrimStart());
            collectingHeader = false;
        } else {
            if (row.StartsWith(spotName)) {
                collecting = true;
                collectingHeader = true;
            }
        }
    }

    var (fishRowHeader, availability) = ParseFishHeader(type, fishRows.First());

    foreach (var row in fishRows.Skip(1)) {
        fish.Add(ParseFish(type, fishRowHeader, availability, row));
    }

    return new Spot {
        Type = (SpotType) spot.RowId,
        IsSpectral = spectral,
        Fish = fish
    };
}

(uint[], List<string>) ParseFishHeader(RouteType type, string header) {
    var baits = new uint[4];

    var parts = header.Split("\t");
    for (var i = 0; i < 4; i++) {
        var name = parts[i + 3];

        // thanks
        var realName = name switch {
            "H. Steel Jig" => "Heavy Steel Jig",
            "Shrimp Cage" => "Shrimp Cage Feeder",
            _ => name
        };

        var bait = LookupByName(realName);
        baits[i] = bait.RowId;
    }

    var availability = new List<string>();
    var len = type switch {
        RouteType.Indigo => 6,
        RouteType.Ruby => 7
    };
    var start = parts.Length - len - 3;

    for (var i = start; i < start + len; i++) {
        if (string.IsNullOrEmpty(parts[i])) break;
        availability.Add(parts[i].Trim());
    }

    return (baits, availability);
}

Item LookupByName(string name) {
    return item.ToList()
               .First(x => {
                   var nameLowercase = name.ToLower();
                   var itemNameLowercase = x.Name.ExtractText().ToLower();
                   return itemNameLowercase == nameLowercase;
               });
}

Fish ParseFish(RouteType type, uint[] baits, List<string> availability, string row) {
    var parts = row.Split("\t");

    var name = parts[0];
    var cellType = CellType.None;
    if (name.Contains('!')) {
        (cellType, name) = ParseCellType(name);
    }

    // thanks
    var realName = name switch {
        "Jade Shrimp" => "Jade Mantis Shrimp",
        _ => name
    };
    var fishItem = LookupByName(realName);

    var unparsedBiteTimes = new[] {
        ParseBiteTime(parts[1]),
        ParseBiteTime(parts[2]),
        ParseBiteTime(parts[3]),
        ParseBiteTime(parts[4])
    };
    var biteTimes = new Dictionary<uint, BiteTime>();

    for (var i = 0; i < 4; i++) {
        var biteTime = unparsedBiteTimes[i];
        if (biteTime == null) continue;
        var bait = baits[i];
        biteTimes[bait] = biteTime;
    }

    var timeAvailability = new Dictionary<Time, bool>();
    var weatherAvailability = new Dictionary<WeatherType, bool>();

    var isTime = availability.Contains("Day")
                 || availability.Contains("Sunset")
                 || availability.Contains("Night");

    if (isTime) {
        for (var i = 0; i < availability.Count; i++) {
            var avail = availability[i];
            var entry = avail switch {
                "Day" => Time.Day,
                "Sunset" => Time.Sunset,
                "Night" => Time.Night,
                _ => throw new Exception("Unknown time: " + avail)
            };

            // Copied from above, I'm lazy
            var len = type switch {
                RouteType.Indigo => 6,
                RouteType.Ruby => 7
            };
            var start = parts.Length - len - 3;

            var value = parts[start + i] == "Yes";
            timeAvailability[entry] = value;
        }
    } else {
        for (var i = 0; i < availability.Count; i++) {
            var avail = availability[i];
            WeatherType? entry = null;
            foreach (var weatherRow in weather) {
                if (avail == weatherRow.Name.ExtractText()) {
                    entry = (WeatherType) weatherRow.RowId;
                    break;
                }
            }

            if (entry == null) throw new Exception("Unknown weather: " + avail);

            // Also copied from above, I'm still lazy
            var len = type switch {
                RouteType.Indigo => 6,
                RouteType.Ruby => 7
            };

            var start = parts.Length - len - 3;

            var value = parts[start + i] == "Yes";
            weatherAvailability[entry.Value] = value;
        }
    }

    // TODO: use MO! detection
    Mooch? mooch = null;
    uint? requiredBait = null;
    if (parts[1].Contains("Mooch")) {
        // Mooch only, 7-13
        // Rothlyt Mussel Mooch, 9+ seconds

        var moochPart = parts[1];
        if (moochPart.Contains('!')) {
            moochPart = moochPart.Substring(moochPart.IndexOf('!') + 1);
        }

        var separator = ", ";
        var separatorIndex = moochPart.IndexOf(separator, StringComparison.Ordinal);

        var beforeSep = moochPart.Substring(0, separatorIndex);
        var afterSep = moochPart.Substring(separatorIndex + separator.Length);

        uint? moochBait = null;
        if (beforeSep != "Mooch only") {
            var moochFish = beforeSep.Split("Mooch")[0].Trim();
            moochFish = moochFish.Replace(star, "");
            moochBait = LookupByName(moochFish).RowId;
        }

        var moochBiteTimes = ParseRange(afterSep);
        if (moochBiteTimes == null) {
            throw new Exception("Failed to parse mooch bite times");
        }

        mooch = new Mooch {
            RequiredBait = moochBait,
            Range = moochBiteTimes
        };

        // Any bite times we parse here are invalid
        biteTimes.Clear();
    } else if (parts[1].Contains(star)) {
        var starPos = parts[1].IndexOf(star, StringComparison.Ordinal);
        var baitName = parts[1].Substring(starPos + star.Length).Trim();
        var bait = LookupByName(baitName);
        requiredBait = bait.RowId;
    }

    var voyageMissionType = parts[^2] switch {
        "S" => VoyageMissionType.Shark,
        "J" => VoyageMissionType.Jellyfish,
        "C" => VoyageMissionType.Crab,
        "F" => VoyageMissionType.Fugu,
        "Q" => VoyageMissionType.Squid,
        "H" => VoyageMissionType.Shrimp,
        "M" => VoyageMissionType.Shellfish,
        _ => VoyageMissionType.None
    };

    return new Fish {
        ItemId = fishItem.RowId,
        CellType = cellType,
        VoyageMissionType = voyageMissionType,
        CanCauseSpectral = fishItem.Name.ExtractText().Contains("Spectral")
                           // Okay man
                           || fishItem.Name.ExtractText() == "Spectresaur",

        RequiredBait = requiredBait,
        BiteTimes = biteTimes,
        DoubleHook = ParseRange(parts[6]),
        TripleHook = ParseRange(parts[7]),

        BitePower = parts[8].ToCharArray().Count(x => x == '!'),
        Hookset = parts[9] == "Powerful" ? Hookset.Powerful : Hookset.Precision,
        AveragePoints = int.Parse(parts[5]),
        Stars = int.Parse(parts[^3]),

        TimeAvailability = timeAvailability,
        WeatherAvailability = weatherAvailability,

        Intuition = ParseIntuition(parts[^1].Trim()),
        Mooch = mooch
    };
}

BiteTime? ParseBiteTime(string str) {
    var cellType = CellType.None;
    if (str.Contains('!')) {
        (cellType, str) = ParseCellType(str);
    }
    var range = ParseRange(str);
    if (range == null) return null;
    return new BiteTime {
        Range = range,
        CellType = cellType
    };
}

(CellType, string) ParseCellType(string str) {
    var exclamation = str.IndexOf("!", StringComparison.Ordinal);
    var cellStr = str.Substring(exclamation + 1);

    var cellTypeStr = str.Substring(0, exclamation);
    var cellType = cellTypeStr switch {
        "B" => CellType.BestOrRequired,
        "U" => CellType.Usable,
        "M" => CellType.Moochable,
        "MD" => CellType.Mooched,
        "MO" => CellType.MoochOnly,
        "I" => CellType.Intuition,
        _ => CellType.None,
    };

    return (cellType, cellStr);
}

Intuition? ParseIntuition(string intuition) {
    if (intuition.StartsWith("Intuition:")) {
        var firstSpace = intuition.IndexOf(" ", StringComparison.Ordinal);
        var lastSpace = intuition.LastIndexOf(" ", StringComparison.Ordinal);

        var durationStr = intuition.Substring(lastSpace + 2, intuition.Length - lastSpace - 2 - 2);
        var duration = int.Parse(durationStr);

        var fishStrs = intuition.Substring(firstSpace + 1, lastSpace - firstSpace - 1).Split(", ");
        var fish = new List<(uint, int)>();
        foreach (var fishy in fishStrs) {
            var fishySpace = fishy.IndexOf(" ", StringComparison.Ordinal);
            var amntStr = fishy.Substring(0, fishySpace);
            var fishName = fishy.Substring(fishySpace + 1);

            int amnt;
            if (amntStr.StartsWith("x")) {
                amnt = int.Parse(amntStr.Substring(1));
            } else {
                amnt = int.Parse(amntStr.Substring(0, amntStr.Length - 1));
            }

            var fishItem = LookupByName(fishName);
            fish.Add((fishItem.RowId, amnt));
        }

        return new Intuition {
            Fish = fish,
            Duration = duration
        };
    }
    return null;
}

Range? ParseRange(string range) {
    // nothing supplied
    if (string.IsNullOrEmpty(range)) return null;

    // no idea how to parse these
    if (range.Contains(star)) return null;

    // 1 - 2
    var twoRegex = new Regex(@"\d+ ?- ?\d+");
    var twoMatch = twoRegex.Match(range);
    if (twoMatch.Success) {
        var parts = twoMatch.Value.Split("-");
        var start = int.Parse(parts[0]);
        var end = int.Parse(parts[1]);
        return new Range {
            Type = Range.RangeType.Range,
            Start = start,
            End = end
        };
    }

    // 1+
    var plusRegex = new Regex(@"\d+ ?\+");
    var plusMatch = plusRegex.Match(range);
    if (plusMatch.Success) {
        var start = int.Parse(plusMatch.Value.Split("+")[0]);
        return new Range {
            Type = Range.RangeType.LooseRange,
            Start = start,
            End = null
        };
    }

    // 1
    var singleRegex = new Regex(@"\d+");
    var singleMatch = singleRegex.Match(range);
    if (singleMatch.Success) {
        var start = int.Parse(singleMatch.Value);
        return new Range {
            Type = Range.RangeType.Single,
            Start = start,
            End = null
        };
    }

    throw new Exception("Unable to parse range: " + range);
}

string Process(RouteType type) {
    // found at the end of the URL for each page
    var gid = type switch {
        RouteType.Indigo => "1841418685",
        RouteType.Ruby => "93698338"
    };

    var url = spreadsheetUrl + gid;
    var tsv = http.GetStringAsync(url).Result;
    var tsvRows = tsv.Split("\n");

    var allSpots = ikdSpot.ToList();
    var spots = type switch {
        RouteType.Indigo => allSpots.Skip(1).Take(7), // 1 to 7
        RouteType.Ruby => allSpots.Skip(8).Take(4)    // 8 to 11
    };

    var arr = new List<Spot>();
    foreach (var spot in spots) {
        arr.Add(ProcessSpot(type, spot, false, tsvRows));
        arr.Add(ProcessSpot(type, spot, true, tsvRows));
    }

    return Serializer.ToString(arr);
}


File.WriteAllText(
    Path.Combine(outPath, "indigo.json"),
    Process(RouteType.Indigo)
);

File.WriteAllText(
    Path.Combine(outPath, "ruby.json"),
    Process(RouteType.Ruby)
);
