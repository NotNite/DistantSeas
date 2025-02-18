using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CheapLoc;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Logging;
using DistantSeas.Common;
using DistantSeas.Core;
using DistantSeas.Tracking;
using DistantSeas.Tracking.LogEntries;
using ImGuiNET;

namespace DistantSeas.Windows.Main;

public class JournalSection : MainWindowSection {
    private List<JournalEntryInfo>? journalEntries = null;
    private HashSet<DateTime> availableDates = new();
    private string selectedFilter = "all";
    private DateTime? selectedDate = null;
    private int maxEntriesToShow = 50;

    public JournalSection() : base(
        FontAwesomeIcon.Book,
        Loc.Localize("JournalSection", "Journal"),
        MainWindowCategory.Tracking
    ) { }

    public override void Draw() {
        var isEnabled = Plugin.Configuration.JournalEnabled;
        var isRunning = isEnabled && Plugin.Journal.IsLogging;
        var greenColor = ImGuiColors.HealerGreen;
        var redColor = ImGuiColors.DalamudRed;

        var activeStr = Loc.Localize("JournalActive", "The current boat is being actively tracked in the journal.");
        var enabledStr = Loc.Localize("JournalEnabled",
                                      "The journal is enabled, but no boat is currently being tracked.");
        var disabledStr = Loc.Localize("JournalDisabled", "The journal is disabled.");

        if (isRunning) {
            using (ImRaii.PushColor(ImGuiCol.Text, greenColor)) {
                Utils.IconText(FontAwesomeIcon.Book, activeStr);
            }
        } else if (isEnabled) {
            Utils.IconText(FontAwesomeIcon.Book, enabledStr);
        } else {
            using (ImRaii.PushColor(ImGuiCol.Text, redColor)) {
                Utils.IconText(FontAwesomeIcon.Book, disabledStr);
            }
        }

        if (
            this.journalEntries == null
            || ImGui.Button(Loc.Localize("JournalSectionRefresh", "Refresh"))
        ) {
            this.UpdateJournalEntries();
            this.UpdateAvailableDates();
        }

        // Filter controls group
        using (ImRaii.Group()) {
            if (ImGui.Button(Loc.Localize("JournalSectionShowAll", "Show All"))) {
                selectedFilter = "all";
                selectedDate = null;
            }
            ImGui.SameLine();
            if (ImGui.Button(Loc.Localize("JournalSectionToday", "Today"))) {
                selectedFilter = "today";
                selectedDate = DateTime.Today;
            }
            ImGui.SameLine();
            if (ImGui.Button(Loc.Localize("JournalSectionLastWeek", "Last 7 Days"))) {
                selectedFilter = "week";
                selectedDate = DateTime.Today.AddDays(-7);
            }
            ImGui.SameLine();

            var dateLabel = selectedDate?.ToString("yyyy-MM-dd") ?? Loc.Localize("JournalSectionSelectDate", "Select Date");
            ImGui.SetNextItemWidth(120);
            if (ImGui.BeginCombo("##DateSelect", dateLabel)) {
                foreach (var date in availableDates.OrderByDescending(d => d)) {
                    var isSelected = selectedDate.HasValue && selectedDate.Value.Date == date.Date;
                    if (ImGui.Selectable(date.ToString("yyyy-MM-dd"), isSelected)) {
                        selectedFilter = "custom";
                        selectedDate = date;
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            ImGui.InputInt(Loc.Localize("JournalSectionMaxEntries", "Max Entries"), ref maxEntriesToShow);
        }

        const ImGuiTableFlags flags = ImGuiTableFlags.Borders
                    | ImGuiTableFlags.Resizable
                    | ImGuiTableFlags.ScrollY
                    | ImGuiTableFlags.RowBg;

        using var table = ImRaii.Table("##DistantSeasJournal", 6, flags);
        if (!table.Success) return;

        ImGui.TableSetupColumn(Loc.Localize("JournalSectionDate", "Date"));
        ImGui.TableSetupColumn(Loc.Localize("JournalSectionRoute", "Route"));
        ImGui.TableSetupColumn(Loc.Localize("JournalSectionTime", "Time"));
        ImGui.TableSetupColumn(Loc.Localize("JournalSectionPoints", "Points"));
        ImGui.TableSetupColumn(Loc.Localize("JournalSectionSpectrals", "Spectrals"));
        ImGui.TableSetupColumn(Loc.Localize("JournalSectionButtons", "Buttons"));
        ImGui.TableHeadersRow();

        var filteredEntries = FilterEntries(this.journalEntries);
        int count = 0;

        foreach (var entry in filteredEntries) {
            if (count >= maxEntriesToShow) break;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            var dateStr = entry.Time.ToString("yyyy-MM-dd hh:mm:ss tt");
            ImGui.TextUnformatted(dateStr);
            ImGui.TableNextColumn();

            ImGui.TextUnformatted(entry.DestinationName);
            ImGui.TableNextColumn();

            ImGui.TextUnformatted(Utils.TimeName(entry.DestinationTime));
            ImGui.TableNextColumn();

            ImGui.TextUnformatted(entry.TotalPoints.ToString());
            ImGui.TableNextColumn();

            ImGui.TextUnformatted(entry.Spectrals.ToString());
            ImGui.TableNextColumn();

            var enabled = ImGui.GetIO().KeyCtrl;

            if (Utils.DisabledButtonWithTooltip(
                    !enabled,
                    Loc.Localize("JournalSectionButtonDelete", "Delete"),
                    disabledText: Loc.Localize("JournalSectionButtonDeleteTooltip",
                                               "This can't be undone. Hold Control to enable this button.")
                )) {
                if (enabled) {
                    File.Delete(entry.Path);
                    this.UpdateJournalEntries();
                    this.UpdateAvailableDates();
                }
            }

            count++;
        }
    }

    private List<JournalEntryInfo> FilterEntries(List<JournalEntryInfo>? entries) {
        if (entries == null) return new List<JournalEntryInfo>();

        return entries.Where(entry => {
            switch (selectedFilter) {
                case "today":
                    return entry.Time.Date == DateTime.Today;
                case "week":
                    return entry.Time.Date >= DateTime.Today.AddDays(-7);
                case "custom":
                    return selectedDate.HasValue && entry.Time.Date == selectedDate.Value.Date;
                default:
                    return true;
            }
        }).ToList();
    }

    private void UpdateAvailableDates() {
        availableDates.Clear();
        if (journalEntries != null) {
            foreach (var entry in journalEntries) {
                availableDates.Add(entry.Time.Date);
            }
        }
    }

    private void UpdateJournalEntries() {
        var path = Plugin.ResourceManager.GetConfigPath("logs");
        if (!Directory.Exists(path)) return;
        var files = Directory.GetFiles(path);

        var newEntries = new List<JournalEntryInfo>();

        foreach (var file in files) {
            try {
                var json = File.ReadAllText(file);
                var entries = Serializer.FromString<List<LogEntry>>(json);
                var fullPath = Path.Combine(path, file);
                var entryInfo = JournalEntryInfo.Parse(fullPath, entries);
                if (entryInfo != null) {
                    newEntries.Add(entryInfo);
                }
            } catch (Exception e) {
                Plugin.PluginLog.Error(e, "Failed to read journal file {File}", file);
            }
        }

        this.journalEntries = newEntries;
    }
}
