using Dalamud.Utility;
using Lumina.Excel.Sheets;
using Lumina.Text;

namespace DistantSeas.Fishing;

public class MissionState {
    public uint Row;
    public string Objective;
    public uint Progress;
    public uint Total;

    public MissionState() { }

    public MissionState(uint rowId) {
        var sheet = Plugin.DataManager.Excel.GetSheet<IKDPlayerMissionCondition>()!;    
        var row = sheet.GetRow(rowId)!;

        this.Row = rowId;
        this.Total = row.Unknown1;
        this.Progress = 0;
        this.Objective = row.Unknown0.ExtractText();
    }
}
