namespace DistantSeas.Common; 

public record ScheduleEntry {
    public DateTime Date;
    public SpotType Destination;
    public Time Time;
}
