using System;
using System.Collections.Generic;
using System.Linq;
using DistantSeas.Common;
using DistantSeas.Tracking.LogEntries;
using Lumina.Excel.Sheets;

namespace DistantSeas.Tracking;

public class JournalEntryInfo {
    public string Path;
    public DateTime Time;
    public string DestinationName;
    public Time DestinationTime;
    public uint TotalPoints;
    public int Spectrals;

    public static JournalEntryInfo? Parse(string path, List<LogEntry> entries) {
        if (entries.FirstOrDefault(x => x is LogEntryHeader) is not LogEntryHeader header) return null;
        if (entries.FirstOrDefault(x => x is LogEntryEnterBoat) is not LogEntryEnterBoat enterBoat) return null;
        if (entries.LastOrDefault(x => x is LogEntryTotalPointsUpdate) is not LogEntryTotalPointsUpdate
            totalPointsUpdate) return null;

        var spectrals = entries.Count(x => x is LogEntrySpectralStarted);

        var ikdRoute = Plugin.DataManager.Excel.GetSheet<IKDRoute>()!;
        var routeRow = ikdRoute.GetRow(enterBoat.Route)!;
        var spot = routeRow.Spot[2].RowId;
        var time = routeRow.Time[2].RowId;

        return new JournalEntryInfo {
            Path = path,
            Time = header.Time,
            DestinationName = Utils.SpotTypeName((SpotType) spot),
            DestinationTime = (Time) time,
            TotalPoints = totalPointsUpdate.TotalPoints,
            Spectrals = spectrals
        };
    }
}
