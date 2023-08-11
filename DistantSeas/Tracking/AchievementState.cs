using System.Collections.Generic;

namespace DistantSeas.Tracking;

public class AchievementState {
    public Dictionary<uint, bool> CompletedAchievements = new();
    public uint TotalPoints = 0;
    public bool IsPointsDirty = true;
}
