using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using DistantSeas.Common;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace DistantSeas.Tracking;

public unsafe class AchievementTracker : IDisposable {
    public List<uint> Achievements = new();
    private AchievementState? cachedState;
    private Timer achievementTimer;
    private const uint OnABoatFive = 2758;
    public const uint OnABoatFiveGoal = 3_000_000;

    private readonly Hook<Achievement.Delegates.ReceiveAchievementProgress> receiveAchievementProgressHook;

    public AchievementTracker() {
        this.receiveAchievementProgressHook = Plugin.GameInteropProvider.HookFromAddress
            <Achievement.Delegates.ReceiveAchievementProgress>(
                Achievement.MemberFunctionPointers
                           .ReceiveAchievementProgress,
                this.ReceiveAchievementProgressDetour
            );
        this.receiveAchievementProgressHook.Enable();

        for (uint i = 2553; i <= 2566; i++) this.Achievements.Add(i);
        for (uint i = 2603; i <= 2606; i++) this.Achievements.Add(i);
        for (uint i = 2748; i <= 2759; i++) this.Achievements.Add(i);
        for (uint i = 3256; i <= 3269; i++) this.Achievements.Add(i);

        this.achievementTimer = new Timer(1000);
        this.achievementTimer.AutoReset = true;
        this.achievementTimer.Elapsed += (_, _) => this.UpdateAchievements();
        this.achievementTimer.Start();

        Plugin.ClientState.Logout += this.OnLogout;
    }

    public void Dispose() {
        this.receiveAchievementProgressHook.Dispose();
        this.achievementTimer.Dispose();
        Plugin.ClientState.Logout -= this.OnLogout;
    }

    private void OnLogout(int type, int code) {
        this.cachedState = null;
    }

    private string GetFilePath() {
        var cid = Plugin.PlayerState.ContentId;
        var hex = cid.ToString("X8");
        return Plugin.ResourceManager.GetConfigPath("achievement", hex + ".json");
    }

    public AchievementState GetState() {
        return this.cachedState ?? this.GetAchievementState();
    }

    public void AddPoints(uint points) {
        var state = this.GetAchievementState();
        state.TotalPoints += points;
        state.IsPointsDirty = true;
        this.WriteAchievementState(state);
    }

    private AchievementState GetAchievementState() {
        var path = this.GetFilePath();
        if (!File.Exists(path)) return new AchievementState();
        var str = File.ReadAllText(path);
        var state = Serializer.FromString<AchievementState>(str);
        this.cachedState = state;
        return state;
    }

    private void WriteAchievementState(AchievementState state) {
        var path = this.GetFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var str = Serializer.ToString(state);
        File.WriteAllText(path, str);
        this.cachedState = state;
    }

    private void ReceiveAchievementProgressDetour(Achievement* achievement, uint id, uint current, uint max) {
        if (id == OnABoatFive) {
            try {
                var state = this.GetAchievementState();
                // Server only sends us up to the goal so it'll reset to 3 mil if we go higher
                if (current < OnABoatFiveGoal) state.TotalPoints = current;
                state.IsPointsDirty = false;
                this.WriteAchievementState(state);
            } catch (Exception e) {
                Plugin.PluginLog.Error("Error receiving achievement progress: {e}", e);
            }
        }

        this.receiveAchievementProgressHook.Original(achievement, id, current, max);
    }

    private void UpdateAchievements() {
        var achievement = Achievement.Instance();
        if (achievement->IsLoaded()) {
            var states = new Dictionary<uint, bool>();

            foreach (var id in this.Achievements) {
                var complete = achievement->IsComplete((int) id);
                states[id] = complete;
            }

            if (this.cachedState == null || this.CompareDict(this.cachedState.CompletedAchievements, states)) {
                var state = this.GetAchievementState();
                state.CompletedAchievements = states;
                this.WriteAchievementState(state);
                Plugin.PluginLog.Debug("Wrote achievements");
            }
        }
    }

    private bool CompareDict(Dictionary<uint, bool> cachedStates, Dictionary<uint, bool> newStates) {
        foreach (var (id, complete) in newStates) {
            if (!cachedStates.ContainsKey(id)) return true;
            if (cachedStates[id] != complete && complete) return true;
        }

        return false;
    }
}
