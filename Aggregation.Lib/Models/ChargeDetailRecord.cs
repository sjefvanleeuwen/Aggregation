namespace Aggregation.Lib.Models;

public class ChargeDetailRecord
{
    public int Id { get; set; }
    public string EvseId { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public string EvId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double KwhUsage { get; set; }
    public decimal TotalCharge { get; set; }

    public TimeSpan ParkingTime => EndTime - StartTime;
}
