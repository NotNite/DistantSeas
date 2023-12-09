namespace DistantSeas.Common;

public class Fish {
    public uint ItemId;
    public CellType CellType;
    public VoyageMissionType VoyageMissionType;
    public bool CanCauseSpectral;
    
    public uint? RequiredBait;
    public Dictionary<uint, BiteTime> BiteTimes;
    public Range? DoubleHook;
    public Range? TripleHook;

    public int BitePower;
    public Hookset Hookset;
    public int AveragePoints;
    public int Stars;

    public Dictionary<Time, bool> TimeAvailability;
    public Dictionary<WeatherType, bool> WeatherAvailability;

    public Intuition? Intuition;
    public Mooch? Mooch;

    public bool IsAvailableInTime(Time time) {
        return !this.TimeAvailability.ContainsKey(time) || this.TimeAvailability[time];
    }

    public bool IsAvailableInWeather(WeatherType weather) {
        return !this.WeatherAvailability.ContainsKey(weather) || this.WeatherAvailability[weather];
    }

    public Fish? GetMoochFish(Spot spot) {
        if (this.Mooch == null) return null;

        if (this.Mooch.RequiredBait != null) {
            var fish = spot.Fish.FirstOrDefault(x => x.ItemId == this.Mooch.RequiredBait);
            if (fish != null) return fish;
        }

        var moochable = spot.Fish.FirstOrDefault(x => x.CellType == CellType.Moochable);
        if (moochable != null) return moochable;

        return null;
    }

    public (HookType, int, int) GetMaxPoints() {
        var maxPoints = (HookType.Single, 0, 0);

        if (this.DoubleHook != null) {
            var dhPoints = this.DoubleHook.End ?? this.DoubleHook.Start;
            if (dhPoints > maxPoints.Item2) maxPoints = (HookType.Double, dhPoints, dhPoints * this.AveragePoints);
        }

        if (this.TripleHook != null) {
            var thPoints = this.TripleHook.End ?? this.TripleHook.Start;
            if (thPoints > maxPoints.Item2) maxPoints = (HookType.Triple, thPoints, thPoints * this.AveragePoints);
        }

        return maxPoints;
    }
}
