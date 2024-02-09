using System;
using System.Timers;
using Dalamud.Plugin.Services;

namespace DistantSeas.Fishing;

public class SpectralTimer {
    private bool spectral = false;
    private uint zone = 0;
    private static readonly uint SpectralTime = 2 * 60; 
    private bool lastZoneHadSpectral;
    private static Timer stopwatch;
    public uint Timer = SpectralTime;
    private uint extraTime = 0;
    public SpectralTimer() {
        stopwatch = new Timer(1000);
        stopwatch.Elapsed += OnTimedEvent;
        stopwatch.AutoReset = true;

        Plugin.EnteredOceanFishing += this.EnteredOceanFishing;
        Plugin.Framework.Update += this.FrameworkUpdate;
    }
    
    private void OnTimedEvent(Object? source, ElapsedEventArgs e) {
        if (this.spectral) {
            this.Timer--;
        }
    }
    
    public void Dispose() {
        stopwatch.Close();
        Plugin.EnteredOceanFishing -= this.EnteredOceanFishing;
        Plugin.Framework.Update -= this.FrameworkUpdate;
    }
    
    private void EnteredOceanFishing() {
        this.Timer = SpectralTime; //new voyage, spectral timer is set to 2 minutes
        this.lastZoneHadSpectral = false; 
        extraTime = 0;

    }
    
    /*

 Thanks to Teamcraft for this information.
 https://guides.ffxivteamcraft.com/guide/ocean-fishing-points#spectral-currents

    The default duration of a spectral current is 2 minutes. They can be extended to a maximum of 3 minutes in two ways:
        If a spectral current is skipped at one stop, the next current that occurs will be extended to 3 minutes.
        This does not stack, meaning skipping multiple spectral currents will not provide any extra benefit over skipping one.

    If a spectral current occurs below 2:30 left on the stop and is cut short, the time that was cut off is added to
    the next one that occurs at the next stop to a maximum of 3 minutes.

    This also applies to extended spectral currents.
    For example, if an extended spectral current that would have been 3 minutes occurs below
     3:30 left on the stop and is cut short, the time that was cut off is added to the next
     one that occurs to a maximum of 3 minutes.

 */
    private void FrameworkUpdate(IFramework framework) {

        var stateTracker = Plugin.StateTracker;
        
        if (stateTracker.IsInOceanFishing) {
            if (Plugin.StateTracker.IsDataLoaded) {
                var newZone = stateTracker.CurrentZone;
                
                if (newZone != this.zone) {
                    this.zone = newZone;
                    if (!this.lastZoneHadSpectral && zone != 0) {
                        this.Timer = 3 * 60;
                    }
                    this.lastZoneHadSpectral = false;
                }
            }
            var newSpectral = stateTracker.IsSpectralActive;
            if (newSpectral != this.spectral) { //spectral state changed
                this.spectral = newSpectral;

                if (this.spectral) {
                    //spec will auto stop when 30s remain on the instance timer
                    if((stateTracker.TimeLeftInZone - 30) < this.Timer) {
                        this.extraTime = (uint) (this.Timer - (stateTracker.TimeLeftInZone - 30));
                        this.Timer = (uint) (stateTracker.TimeLeftInZone - 30);
                    }
                    stopwatch.Start();
                    this.lastZoneHadSpectral = true;

                } else {
                    stopwatch.Stop();
                    this.Timer = Math.Min(SpectralTime + this.extraTime, 3 * 60);
                }
                
            }
        }
    }

}
