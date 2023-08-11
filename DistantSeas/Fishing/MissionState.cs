using Dalamud.Utility;
using Lumina.Text;

namespace DistantSeas.Fishing;

public class MissionState {
    public uint Row;
    public string Objective;
    public uint Progress;
    public uint Total;

    public MissionState() { }

    public MissionState(uint rowId) {
        var sheet = Plugin.DataManager.Excel.GetSheetRaw("IKDPlayerMissionCondition")!;
        var row = sheet.GetRow(rowId)!;

        this.Row = rowId;
        this.Total = row.ReadColumn<byte>(0);
        this.Progress = 0;
        this.Objective = row.ReadColumn<SeString>(1)!.ToDalamudString().TextValue;
    }
}
