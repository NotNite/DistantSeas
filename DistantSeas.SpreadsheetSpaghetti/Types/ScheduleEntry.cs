namespace DistantSeas.SpreadsheetSpaghetti.Types; 

public record ScheduleEntry {
    public DateTime Date;
    public SpotType Destination;
    public Time Time;
}
