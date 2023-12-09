using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CheapLoc;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using DistantSeas.Common;
using DistantSeas.Core;
using ImGuiNET;

namespace DistantSeas.Windows.Main;

public class SchedulesSection : MainWindowSection {
    private int routeType;
    private int count = 12;
    private List<ScheduleEntry>? schedules;

    public SchedulesSection() : base(
        FontAwesomeIcon.Clock,
        Loc.Localize("SchedulesSection", "Schedules"),
        MainWindowCategory.None
    ) { }

    public override void Draw() {
        if (this.schedules == null) {
            this.UpdateSchedules(true);
        }

        var flags = ImGuiTableFlags.Borders
                    | ImGuiTableFlags.Resizable
                    | ImGuiTableFlags.ScrollY
                    | ImGuiTableFlags.RowBg;
        
        if (ImGui.Combo(
                Loc.Localize("SchedulesSectionRoute", "Route"),
                ref this.routeType,
                new[] {
                    "Indigo",
                    "Ruby"
                },
                2
            )) {
            this.UpdateSchedules(true);
        }

        if (ImGui.InputInt(
                Loc.Localize("SchedulesSectionAmount", "Amount of schedules to show"),
                ref this.count
            )) {
            if (this.count < 1) this.count = 1;
            if (this.count > this.schedules!.Count) this.count = this.schedules.Count;
        }
        
        var clippedSchedules = this.schedules!.Take(this.count).ToList();
        using (ImRaii.Table("##DistantSeasSchedule", 3, flags)) {
            ImGui.TableSetupColumn(Loc.Localize("SchedulesSectionRoute", "Route"));
            ImGui.TableSetupColumn(Loc.Localize("SchedulesSectionTime", "Time"));
            ImGui.TableSetupColumn(Loc.Localize("SchedulesSectionDate", "Date"));

            ImGui.TableHeadersRow();

            foreach (var schedule in clippedSchedules) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.TextUnformatted(Utils.SpotTypeName(schedule.Destination));
                ImGui.TableNextColumn();

                ImGui.TextUnformatted(Utils.TimeName(schedule.Time));
                ImGui.TableNextColumn();

                var inLocalTz = schedule.Date.ToLocalTime();
                var relativeDate = this.BuildRelativeDate(inLocalTz);
                ImGui.TextUnformatted(relativeDate);
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(inLocalTz.ToString(CultureInfo.CurrentCulture));
            }
        }
    }

    private string BuildRelativeDate(DateTime time) {
        var past = Loc.Localize("RelativeDatePast", "in the past");
        var boarding = Loc.Localize("RelativeDateBoarding", "boarding");
        var seconds = Loc.Localize("RelativeDateSeconds", "in {0} seconds");
        var minutes = Loc.Localize("RelativeDateMinutes", "in {0} minutes");
        var hours = Loc.Localize("RelativeDateHours", "in {0} hours");
        var days = Loc.Localize("RelativeDateDays", "in {0} days");

        // 15 minute window to board
        if (time < DateTime.Now - TimeSpan.FromMinutes(15)) return past;
        if (time < DateTime.Now) return boarding;

        var left = time - DateTime.Now;

        if (left < TimeSpan.FromMinutes(1))
            return string.Format(seconds, left.Seconds + Math.Ceiling(left.Milliseconds / 1000.0));

        if (left < TimeSpan.FromHours(1))
            return string.Format(minutes, left.Minutes + Math.Ceiling(left.Seconds / 60.0));

        if (left < TimeSpan.FromDays(1))
            return string.Format(hours, left.Hours + Math.Ceiling(left.Minutes / 60.0));

        return string.Format(days, left.Days);
    }

    private void UpdateSchedules(bool force) {
        this.schedules = Plugin.FishData.GetSchedule((RouteType) this.routeType);
    }
}
