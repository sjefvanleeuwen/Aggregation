using Aggregation.Lib.Models;

namespace Aggregation.Lib.Services;

public class DataGenerator
{
    private readonly Random _random = new(42); // Using a seed for reproducible results

    public List<EvseLocation> GenerateLocations(int count)
    {
        var locations = new List<EvseLocation>();
        
        for (int i = 1; i <= count; i++)
        {
            var evsesPerLocation = _random.Next(1, 6); // 1 to 5 chargers per location
            var evseIds = new List<string>();
            
            for (int j = 1; j <= evsesPerLocation; j++)
            {
                evseIds.Add($"EVSE-L{i:D3}-{j:D2}");
            }
            
            locations.Add(new EvseLocation
            {
                LocationId = i,
                Name = $"Charging Station {i}",
                Address = $"{_random.Next(1, 1000)} Main St, City {i % 20 + 1}",
                EvseIds = evseIds
            });
        }
        
        return locations;
    }

    public List<EvDriver> GenerateDrivers(int count)
    {
        var drivers = new List<EvDriver>();
        
        for (int i = 1; i <= count; i++)
        {
            drivers.Add(new EvDriver
            {
                EvId = $"EV-{i:D3}",
                DriverName = $"Driver {i}"
            });
        }
        
        return drivers;
    }

    public List<ChargeDetailRecord> GenerateChargeRecords(
        List<EvseLocation> locations, 
        List<EvDriver> drivers, 
        int recordsCount)
    {
        var records = new List<ChargeDetailRecord>();
        var startDate = DateTime.Now.AddMonths(-1);
        
        for (int i = 1; i <= recordsCount; i++)
        {
            // Randomly select a location
            var location = locations[_random.Next(locations.Count)];
            
            // Randomly select an EVSE from that location
            var evseId = location.EvseIds[_random.Next(location.EvseIds.Count)];
            
            // Randomly select a driver
            var driver = drivers[_random.Next(drivers.Count)];
            
            // Generate random start time within the last month
            var startTime = startDate.AddMinutes(_random.Next(0, 60 * 24 * 30));
            
            // Generate random charging duration between 30 minutes and 4 hours
            var chargingDurationMinutes = _random.Next(30, 240);
            var endTime = startTime.AddMinutes(chargingDurationMinutes);
            
            // Generate random kWh usage based on duration (roughly 7-15 kW charging rate)
            var chargingRate = _random.NextDouble() * (15 - 7) + 7; // kW between 7 and 15
            var kwhUsage = Math.Round(chargingRate * chargingDurationMinutes / 60, 2);
            
            // Calculate total charge (price per kWh between $0.15 and $0.30)
            var pricePerKwh = (decimal)(_random.NextDouble() * (0.30 - 0.15) + 0.15);
            var totalCharge = Math.Round((decimal)kwhUsage * pricePerKwh, 2);
            
            records.Add(new ChargeDetailRecord
            {
                Id = i,
                EvseId = evseId,
                LocationId = location.LocationId,
                EvId = driver.EvId,
                StartTime = startTime,
                EndTime = endTime,
                KwhUsage = kwhUsage,
                TotalCharge = totalCharge
            });
        }
        
        return records;
    }
}
