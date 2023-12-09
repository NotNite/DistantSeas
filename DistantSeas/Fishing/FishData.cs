using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Logging;
using DistantSeas.Common;

namespace DistantSeas.Fishing;

// Part of this code is copied from pillowfication's website:
// https://github.com/pillowfication/ffxiv/blob/master/src/ocean-fishing/ffxiv-ocean-fishing/src/calculate-voyages.ts
// No license, but I was granted permission in the Fisherman's Horizon server
public class FishData {
    public List<Spot> Spots = new();
    public List<Fish> Fish = new();
    public List<uint> Baits = new();
    public Dictionary<RouteType, List<ScheduleEntry>> Patterns = new();

    private const long _9Hours = 32400000;
    private const long _45Min = 2700000;
    private static long Epoch = 1593270000000 + _9Hours;

    private static Dictionary<RouteType, List<SpotType>> RouteCycle = new() {
        {
            RouteType.Indigo,
            new() {SpotType.BloodbrineSea, SpotType.RothlytSound, SpotType.NorthernMerlthor, SpotType.RhotanoSea}
        }, {
            RouteType.Ruby,
            new() {SpotType.OneRiver, SpotType.RubySea}
        }
    };

    private static Dictionary<RouteType, List<Time>> TimeCycle = new() {
        {
            RouteType.Indigo,
            new() {
                Time.Sunset, Time.Sunset, Time.Sunset, Time.Sunset, Time.Night, Time.Night, Time.Night, Time.Night,
                Time.Day, Time.Day, Time.Day, Time.Day
            }
        }, {
            RouteType.Ruby,
            new() {Time.Day, Time.Day, Time.Sunset, Time.Sunset, Time.Night, Time.Night}
        }
    };

    public FishData() {
        this.PopulateSpots();
        this.PopulateSchedules();
        this.PopulateFishAndBaits();
    }

    private void PopulateSpots() {
        var indigoStr = File.ReadAllText(Plugin.ResourceManager.GetDataPath("indigo.json"));
        var rubyStr = File.ReadAllText(Plugin.ResourceManager.GetDataPath("ruby.json"));

        var indigo = Serializer.FromString<List<Spot>>(indigoStr);
        var ruby = Serializer.FromString<List<Spot>>(rubyStr);

        this.Spots.AddRange(indigo);
        this.Spots.AddRange(ruby);
    }

    private void PopulateSchedules() {
        this.Patterns.Clear();

        foreach (var routeType in Enum.GetValues<RouteType>()) {
            var schedule = this.CalculateSchedule(routeType);
            this.Patterns.Add(routeType, schedule);
        }
    }

    private void PopulateFishAndBaits() {
        this.Fish = this.Spots
                        .Select(spot => spot.Fish)
                        .SelectMany(fish => fish)
                        .ToList();

        this.Baits = this.Fish
                         .Select(x => x.BiteTimes.Keys.ToArray())
                         .SelectMany(x => x)
                         .ToList();
    }

    private List<ScheduleEntry> CalculateSchedule(RouteType routeType) {
        var destCycle = RouteCycle[routeType];
        var timeCycle = TimeCycle[routeType];

        // Subtract 45 minutes to catch ongoing voyages
        var date = DateTimeOffset.UnixEpoch.AddMilliseconds(_45Min);
        var adjustedDate = date.AddMilliseconds(_9Hours - _45Min);
        var day = (int) Math.Floor((adjustedDate.ToUnixTimeMilliseconds() - Epoch) / 86400000.0);
        var hour = adjustedDate.Hour;

        // Adjust hour to be odd
        hour += (hour & 1) == 0 ? 1 : 2;
        if (hour > 23) {
            day += 1;
            hour -= 24;
        }

        // Find the current voyage
        var voyageNumber = hour >> 1;
        var destIndex = (((day + voyageNumber) % destCycle.Count) + destCycle.Count) % destCycle.Count;
        var timeIndex = (((day + voyageNumber) % timeCycle.Count) + timeCycle.Count) % timeCycle.Count;

        // Loop until however many voyages are found
        var upcomingVoyages = new List<ScheduleEntry>();
        while (upcomingVoyages.Count < 144) {
            var destination = destCycle[destIndex];
            var time = timeCycle[timeIndex];

            upcomingVoyages.Add(new ScheduleEntry {
                Date = this.FromEpoch(day, hour),
                Destination = destination,
                Time = time
            });

            if (hour == 23) {
                day += 1;
                hour = 1;
                destIndex = (destIndex + 2) % destCycle.Count;
                timeIndex = (timeIndex + 2) % timeCycle.Count;
            } else {
                hour += 2;
                destIndex = (destIndex + 1) % destCycle.Count;
                timeIndex = (timeIndex + 1) % timeCycle.Count;
            }
        }

        return upcomingVoyages;
    }

    private DateTime FromEpoch(int day, int hour) {
        return DateTimeOffset.FromUnixTimeMilliseconds(
            Epoch + (day * 86400000) + (hour * 3600000) - _9Hours
        ).UtcDateTime;
    }

    public List<ScheduleEntry> GetSchedule(RouteType routeType) {
        var date = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // -1 to catch ongoing voyages
        var offset = date - _45Min;
        var startIndex = (long) Math.Floor(offset / 7200000.0);

        var upcomingVoyages = new List<ScheduleEntry>();

        for (var i = 0; upcomingVoyages.Count < 144; i++) {
            var idx = (int) ((startIndex + i) % 144);
            var scheduleEntry = this.Patterns[routeType][idx];
            var fixedDate = (startIndex + i + 1) * 7200000;

            upcomingVoyages.Add(scheduleEntry with {
                Date = DateTimeOffset.FromUnixTimeMilliseconds(fixedDate).DateTime
            });
        }

        return upcomingVoyages;
    }

    public ScheduleEntry GetNextRouteTime() {
        var schedule = this.GetSchedule(RouteType.Indigo);
        foreach (var entry in schedule) {
            var inLocalTz = entry.Date.ToLocalTime();
            var now = DateTime.Now;
            if (inLocalTz > now) {
                return entry;
            }
        }

        PluginLog.Warning("Ran out of routes in FishData#GetNextRouteTime");
        return null!;
    }

    public List<Fish> FilterForVoyageMission(List<Fish> fishes, List<MissionState> missions) {
        var ret = new List<Fish>();
        foreach (var fish in fishes) {
            var meetsAny = false;
            foreach (var mission in missions) {
                if (Plugin.Configuration.HideFinishedMissions && mission.Progress >= mission.Total) {
                    continue;
                }

                // IKDPlayerMissionCondition
                var meetsMission = mission.Row switch {
                    // Catch fish with a weak bite (!)
                    4 or 10 or 16 or 22 or 27 or 32 => fish.BitePower == 1,
                    // Catch fish with a strong bite (!!)
                    5 or 11 or 17 or 23 or 28 or 33 => fish.BitePower == 2,
                    // Catch fish with a ferocious bite (!!!)
                    6 or 12 or 18 or 24 or 29 or 34 => fish.BitePower == 3,

                    // Catch fish rated ★★★ or higher
                    2 or 8 or 14 or 20 or 26 or 31 => fish.Stars >= 3,

                    // Catch jellyfish or crabs
                    1 => fish.VoyageMissionType is VoyageMissionType.Jellyfish or VoyageMissionType.Crab,
                    // Catch sharks
                    7 => fish.VoyageMissionType is VoyageMissionType.Shark,
                    // Catch crabs
                    13 => fish.VoyageMissionType is VoyageMissionType.Crab,
                    // Catch fugu
                    19 => fish.VoyageMissionType is VoyageMissionType.Fugu,
                    // Catch shrimp or squid
                    25 => fish.VoyageMissionType is VoyageMissionType.Shrimp or VoyageMissionType.Squid,
                    // Catch shrimp or shellfish
                    30 => fish.VoyageMissionType is VoyageMissionType.Shrimp or VoyageMissionType.Shellfish,

                    _ => false
                };

                if (meetsMission) meetsAny = true;
            }
            if (meetsAny) ret.Add(fish);
        }

        return ret;
    }
}
