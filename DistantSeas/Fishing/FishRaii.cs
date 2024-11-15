using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using DistantSeas.Common;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Action = System.Action;

namespace DistantSeas.Fishing;

public class FishRaii : IDisposable {
    private readonly ExcelSheet<IKDRoute> ikdRouteSheet;
    private readonly ExcelSheet<IKDSpot> ikdSpotSheet;
    private readonly ExcelSheet<Item> itemSheet;

    private readonly Timer timer;

    private Spot? spot;
    private List<Fish>? availableFish;
    private List<Fish>? voyageFish;
    private List<Item>? usableBait;

    public Action? Updated;

    public bool Enabled {
        get => this.timer.Enabled;
        set {
            if (this.Enabled && !value)
                this.Clear();
            this.timer.Enabled = value;
        }
    }

    public FishRaii() {
        this.ikdRouteSheet = Plugin.DataManager.Excel.GetSheet<IKDRoute>()!;
        this.ikdSpotSheet = Plugin.DataManager.Excel.GetSheet<IKDSpot>()!;
        this.itemSheet = Plugin.DataManager.Excel.GetSheet<Item>()!;

        this.timer = new Timer(1000);
        this.timer.AutoReset = true;
        this.timer.Elapsed += (_, _) => this.Update();
    }

    public void Dispose() {
        this.timer.Dispose();
        this.Clear();
    }

    public void Update() {
        this.UpdateSpot();
        this.UpdateFish();
    }

    private void Clear() {
        this.spot = null;
        this.availableFish = null;
        this.voyageFish = null;
        this.usableBait = null;
        this.Updated = null;
    }

    // Spot

    public Spot GetSpot() => this.spot ?? this.UpdateSpot();

    private Spot UpdateSpot() => this.spot = Plugin.BaitManager.GetCurrentSpot();

    // Fish list

    public List<Fish> GetAvailableFish() {
        if (this.availableFish == null)
            this.UpdateFish();
        return this.availableFish!;
    }

    public List<Fish> GetVoyageFish() {
        if (this.voyageFish == null)
            this.UpdateFish();
        return this.voyageFish!;
    }

    public List<Item> GetUsableBait() {
        if (this.usableBait == null)
            this.UpdateFish();
        return this.usableBait!;
    }

    private void UpdateFish() {
        var available = Plugin.BaitManager.GetAvailableFish();
        this.availableFish = available;

        var missionState = Plugin.StateTracker.MissionState;
        var voyage = Plugin.FishData.FilterForVoyageMission(available, missionState);
        this.voyageFish = voyage;

        this.usableBait = available.Select(fish => Plugin.BaitManager.GetBaitChain(fish.ItemId).First())
                                   .Where(id => id != 0)
                                   .Distinct()
                                   .Order()
                                   .Select(id => this.itemSheet.GetRow(id)!)
                                   .ToList();

        this.Updated?.Invoke();
    }

    // Spectral fish

    private static bool SpectralPredicate(Fish fish) => fish.CanCauseSpectral;

    public bool HasSpectralFish() => this.GetAvailableFish().Any(SpectralPredicate);

    public List<Fish> GetSpectralFish() => this.GetAvailableFish()
                                               .Where(SpectralPredicate)
                                               .ToList();

    // Intuition fish

    private static bool IntuitionPredicate(Fish fish) => fish.Intuition != null;

    public bool HasIntuitionFish() => this.GetAvailableFish().Any(IntuitionPredicate);

    public List<Fish> GetIntuitionFish() => this.GetAvailableFish()
                                                .Where(IntuitionPredicate)
                                                .ToList();

    // Route

    public RowRef<IKDSpot> GetCurrentSpot() {
        var route = this.ikdRouteSheet.GetRow(Plugin.StateTracker.CurrentRoute);
        return route.Spot[(int) Plugin.BaitManager.CurrentZone];
    }
    
    public RowRef<IKDTimeDefine> GetCurrentTime() {
        var route = this.ikdRouteSheet.GetRow(Plugin.StateTracker.CurrentRoute);
        return route.Time[(int) Plugin.BaitManager.CurrentZone];
    }

    // Items

    public Item GetItem(uint id) => this.itemSheet.GetRow(id)!;

    public Item GetFishItem(Fish fish) => this.itemSheet.GetRow(fish.ItemId)!;

    public Item? GetCurrentBaitItem() {
        var baitId = Plugin.BaitManager.CurrentBait;
        return baitId != 0 ? this.itemSheet.GetRow(baitId) : null;
    }
}
