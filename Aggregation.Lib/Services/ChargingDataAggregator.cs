using Aggregation.Lib.Models;

namespace Aggregation.Lib.Services;

public class ChargingDataAggregator
{
    public class LocationAggregation
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public TimeSpan TotalParkingTime { get; set; }
        public double TotalKwhUsage { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class DriverAggregation
    {
        public string EvId { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public TimeSpan TotalParkingTime { get; set; }
        public double TotalKwhUsage { get; set; }
        public decimal TotalSpent { get; set; }
        public Dictionary<int, int> LocationVisits { get; set; } = new Dictionary<int, int>();
    }

    public List<LocationAggregation> AggregateByLocation(
        List<ChargeDetailRecord> records, 
        List<EvseLocation> locations)
    {
        return locations
            .Select(location => new LocationAggregation
            {
                LocationId = location.LocationId,
                LocationName = location.Name,
                TotalSessions = records.Count(r => r.LocationId == location.LocationId),
                TotalParkingTime = TimeSpan.FromTicks(
                    records.Where(r => r.LocationId == location.LocationId)
                        .Sum(r => r.ParkingTime.Ticks)),
                TotalKwhUsage = records.Where(r => r.LocationId == location.LocationId)
                    .Sum(r => r.KwhUsage),
                TotalRevenue = records.Where(r => r.LocationId == location.LocationId)
                    .Sum(r => r.TotalCharge)
            })
            .ToList();
    }

    public List<DriverAggregation> AggregateByDriver(
        List<ChargeDetailRecord> records, 
        List<EvDriver> drivers)
    {
        return drivers
            .Select(driver => 
            {
                var driverRecords = records.Where(r => r.EvId == driver.EvId).ToList();
                
                return new DriverAggregation
                {
                    EvId = driver.EvId,
                    DriverName = driver.DriverName,
                    TotalSessions = driverRecords.Count,
                    TotalParkingTime = TimeSpan.FromTicks(driverRecords.Sum(r => r.ParkingTime.Ticks)),
                    TotalKwhUsage = driverRecords.Sum(r => r.KwhUsage),
                    TotalSpent = driverRecords.Sum(r => r.TotalCharge),
                    LocationVisits = driverRecords
                        .GroupBy(r => r.LocationId)
                        .ToDictionary(g => g.Key, g => g.Count())
                };
            })
            .ToList();
    }
}
