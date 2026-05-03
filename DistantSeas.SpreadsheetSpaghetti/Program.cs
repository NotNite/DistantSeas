using System.Text;
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
    "https://docs.google.com/spreadsheets/d/1uuK0E6pfBtW0Jdlexy8gPbT1p5WTr_Cruv7mmgR3BH0" + "/export?format=tsv&gid=";

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
        if (name == "Mooch") continue;

        var bait = LookupByName(name);
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
    var realName = name switch {
        "H. Steel Jig" => "Heavy Steel Jig",
        "Shrimp Cage" => "Shrimp Cage Feeder",
        "Jade Shrimp" => "Jade Mantis Shrimp",
        "Cieldales Roosterfish" => "Cieldalaes roosterfish",
        _ => name
    };
    realName = realName.ToLower();
    return item.First(x => x.Name.ExtractText().ToLower() == realName);
}

Fish ParseFish(RouteType type, uint[] baits, List<string> availability, string row) {
    var parts = row.Split("\t");

    var name = parts[0];
    var cellType = CellType.None;
    if (name.Contains('!')) {
        (cellType, name) = ParseCellType(name);
    }

    var fishItem = LookupByName(name);
    var biteTimes = new Dictionary<uint, BiteTime>();

    Mooch? mooch = null;
    uint? requiredBait = null;
    if (parts[1].StartsWith("MO!")) {
        var moochPart = parts[1].Substring("MO!".Length);
        if (moochPart.StartsWith(star)) {
            moochPart = moochPart.Substring(star.Length);
        }

        // Mooch only, 7-13
        // Rothlyt Mussel Mooch, 9+ seconds
        // Cieldalaes Roosterfish Mooch, ~6 ~ 8s

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
    } else {
        for (var i = 0; i < 4; i++) {
            var part = parts[i + 1];
            if (part.StartsWith("MO!")) {
                // TODO: handle this weird mooch case
                continue;
            }

            var biteTime = ParseBiteTime(parts[i + 1]);
            if (biteTime == null) continue;

            var bait = baits[i];
            if (bait == 0) throw new Exception("what");
            biteTimes[bait] = biteTime;
        }

        if (parts[1].Contains(star)) {
            var starPos = parts[1].IndexOf(star, StringComparison.Ordinal);
            var baitName = parts[1].Substring(starPos + star.Length).Trim();
            var bait = LookupByName(baitName);
            requiredBait = bait.RowId;
        }
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

    var voyageMissionType = parts[^2] switch {
        "S" => VoyageMissionType.Shark,
        "J" => VoyageMissionType.Jellyfish,
        "C" => VoyageMissionType.Crab,
        "F" => VoyageMissionType.Fugu,
        "Q" => VoyageMissionType.Squid,
        "H" => VoyageMissionType.Shrimp,
        "M" => VoyageMissionType.Shellfish,
        "N" => VoyageMissionType.MantisShrimp,
        "P" => VoyageMissionType.PrehistoricWavekin,
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
    const string prefix = "Intuition: ";
    if (intuition.StartsWith(prefix)) {
        intuition = intuition.Substring(prefix.Length);

        // achievement info which we don't care about
        const string countsFor = "Counts for";
        if (intuition.Contains(countsFor)) {
            intuition = intuition.Substring(0, intuition.IndexOf(countsFor, StringComparison.Ordinal));
        }

        intuition = intuition.Trim();

        var parenthesis = intuition.IndexOf("(", StringComparison.Ordinal);
        var durationStr = intuition.Substring(parenthesis + 1, intuition.Length - parenthesis - 2).Trim();
        if (durationStr.EndsWith("s")) durationStr = durationStr[..^1];
        var duration = int.Parse(durationStr);

        var fishStrs = intuition.Substring(0, parenthesis).Trim().Split(", ");
        var fish = new List<(uint, int)>();
        foreach (var fishy in fishStrs) {
            var fishySpace = fishy.IndexOf(" ", StringComparison.Ordinal);
            var amntStr = fishy.Substring(0, fishySpace);
            var fishName = fishy.Substring(fishySpace + 1);

            if (amntStr.StartsWith("x")) amntStr = amntStr[1..];
            if (amntStr.EndsWith("x")) amntStr = amntStr[..^1];
            var amnt = int.Parse(amntStr);

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

    range = range.Trim();

    // 1 - 2
    var twoRegex = new Regex(@"^~?(\d*\.?\d+)s? ?[-~] ?~?(\d*\.?\d+)s?$");
    var twoMatch = twoRegex.Match(range);
    if (twoMatch.Success) {
        var start = float.Parse(twoMatch.Groups[1].Value);
        var end = float.Parse(twoMatch.Groups[2].Value);
        // Console.WriteLine($"{range} -> {start} {end}");
        return new Range {
            Type = Range.RangeType.Range,
            Start = start,
            End = end
        };
    }

    // 1 and 1+
    var oneRegex = new Regex(@"^~?(\d*\.?\d+)s? ?(\+)?$");
    var oneMatch = oneRegex.Match(range);
    if (oneMatch.Success) {
        var start = float.Parse(oneMatch.Groups[1].Value);
        var isLooseRange = !string.IsNullOrWhiteSpace(oneMatch.Groups[2].Value);
        var type = isLooseRange ? Range.RangeType.LooseRange : Range.RangeType.Single;
        // Console.WriteLine($"{range} -> {start} {type}");
        return new Range {
            Type = type,
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
        RouteType.Ruby => allSpots.Skip(8).Take(6)    // 8 to 13
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
