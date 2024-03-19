using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using DistantSeas.Common;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace DistantSeas.Fishing;

public class FishRaii : IDisposable {
    private readonly ExcelSheet<IKDRoute> ikdRouteSheet;
    private readonly ExcelSheet<IKDSpot> ikdSpotSheet;
    private readonly ExcelSheet<Item> itemSheet;

    private readonly Timer timer;

    private Spot? spot;
    private List<Fish>? availableFish;
    private List<Fish>? voyageFish;

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

    private void UpdateFish() {
        var available = Plugin.BaitManager.GetAvailableFish();
        this.availableFish = available;

        var missionState = Plugin.StateTracker.MissionState;
        var voyage = Plugin.FishData.FilterForVoyageMission(this.availableFish, missionState);
        this.voyageFish = voyage;
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

    public IKDRoute.IKDRouteUnkData0Obj GetCurrentZoneInfo() {
        var route = this.ikdRouteSheet.GetRow(Plugin.StateTracker.CurrentRoute);
        return route!.UnkData0[Plugin.BaitManager.CurrentZone];
    }

    public IKDSpot GetZoneFrom(IKDRoute.IKDRouteUnkData0Obj zoneInfo) {
        return this.ikdSpotSheet.GetRow(zoneInfo.Spot)!;
    }
    
    // Items

    public Item GetItem(uint id) => this.itemSheet.GetRow(id)!;

    public Item GetFishItem(Fish fish) => this.itemSheet.GetRow(fish.ItemId)!;

    public Item? GetCurrentBaitItem() {
        var baitId = Plugin.BaitManager.CurrentBait;
        return baitId != 0 ? this.itemSheet.GetRow(baitId) : null;
    }
}
