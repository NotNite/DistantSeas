using System;
using CheapLoc;
using Dalamud.Plugin.Services;
using DistantSeas.Common;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace DistantSeas.Fishing;

public class AlarmManager : IDisposable {
    private ScheduleEntry nextRoute;
    private ScheduleEntry? lastAlertedFor;

    public AlarmManager() {
        this.nextRoute = Plugin.FishData.GetNextRouteTime();
        Plugin.Framework.Update += this.FrameworkUpdate;
    }

    public void Dispose() {
        Plugin.Framework.Update -= this.FrameworkUpdate;
    }

    private void FrameworkUpdate(IFramework framework) {
        if (!Plugin.Configuration.AlarmEnabled) return;
        if (!Plugin.ClientState.IsLoggedIn) return;

        this.nextRoute = Plugin.FishData.GetNextRouteTime();
        if (this.lastAlertedFor == this.nextRoute) return;

        var diff = this.CalculateDiff();
        if (diff < Plugin.Configuration.AlarmMinutes) {
            this.DispatchAlarm();
            this.lastAlertedFor = this.nextRoute;
        }
    }

    private int CalculateDiff() {
        var inLocalTz = this.nextRoute.Date.ToLocalTime();
        var now = DateTime.Now;
        var diff = inLocalTz - now;
        return (int) diff.TotalMinutes;
    }

    public void DispatchAlarm() {
        if (Plugin.Configuration.AlarmSoundEnabled) {
            UIModule.PlayChatSoundEffect((uint) Plugin.Configuration.AlarmSound);
        }

        var msg = Loc.Localize("AlarmMessage", "[Distant Seas] Next route in {0} minutes.");
        Plugin.ChatGui.Print(string.Format(msg, this.CalculateDiff() + 1)); // why +1 lol
    }

    public void ClearState() {
        this.lastAlertedFor = null;
    }
}
