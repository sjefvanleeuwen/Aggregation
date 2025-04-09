using Aggregation.Lib.Models;
using Aggregation.Lib.Services;

// Initialize data generator and aggregator
var dataGenerator = new DataGenerator();
var aggregator = new ChargingDataAggregator();

// Generate sample data
Console.WriteLine("Generating sample data...");
var locations = dataGenerator.GenerateLocations(100);
var drivers = dataGenerator.GenerateDrivers(10);
var chargeRecords = dataGenerator.GenerateChargeRecords(locations, drivers, 1000);

Console.WriteLine($"Generated {locations.Count} locations with various charging stations");
Console.WriteLine($"Generated {drivers.Count} EV drivers");
Console.WriteLine($"Generated {chargeRecords.Count} charging sessions\n");

// Perform aggregations
Console.WriteLine("Performing aggregations...");
var locationAggregations = aggregator.AggregateByLocation(chargeRecords, locations);
var driverAggregations = aggregator.AggregateByDriver(chargeRecords, drivers);

// Display location aggregations
Console.WriteLine("\n=== LOCATION AGGREGATIONS ===");
Console.WriteLine("{0,-5} {1,-20} {2,-15} {3,-20} {4,-15} {5,-15}", 
    "ID", "Location", "Sessions", "Total Time (h)", "Total kWh", "Revenue");
Console.WriteLine(new string('-', 95));

foreach (var agg in locationAggregations.OrderByDescending(l => l.TotalRevenue).Take(15))
{
    Console.WriteLine("{0,-5} {1,-20} {2,-15} {3,-20:F2} {4,-15:F2} {5,-15:C2}", 
        agg.LocationId, 
        agg.LocationName.Length > 18 ? agg.LocationName.Substring(0, 18) + "..." : agg.LocationName, 
        agg.TotalSessions,
        agg.TotalParkingTime.TotalHours,
        agg.TotalKwhUsage,
        agg.TotalRevenue);
}

// Display driver aggregations
Console.WriteLine("\n=== DRIVER AGGREGATIONS ===");
Console.WriteLine("{0,-8} {1,-15} {2,-15} {3,-20} {4,-15} {5,-15}", 
    "EV ID", "Driver", "Sessions", "Total Time (h)", "Total kWh", "Total Spent");
Console.WriteLine(new string('-', 95));

foreach (var agg in driverAggregations.OrderByDescending(d => d.TotalKwhUsage))
{
    Console.WriteLine("{0,-8} {1,-15} {2,-15} {3,-20:F2} {4,-15:F2} {5,-15:C2}", 
        agg.EvId, 
        agg.DriverName, 
        agg.TotalSessions,
        agg.TotalParkingTime.TotalHours,
        agg.TotalKwhUsage,
        agg.TotalSpent);
    
    // Show top 3 most visited locations for each driver
    Console.WriteLine("    Most visited locations: " + 
                      string.Join(", ", agg.LocationVisits
                          .OrderByDescending(kv => kv.Value)
                          .Take(3)
                          .Select(kv => $"Location {kv.Key} ({kv.Value} visits)")));
}

// Show overall statistics
Console.WriteLine("\n=== OVERALL STATISTICS ===");
Console.WriteLine($"Total charging sessions: {chargeRecords.Count}");
Console.WriteLine($"Total energy delivered: {chargeRecords.Sum(r => r.KwhUsage):F2} kWh");
Console.WriteLine($"Total revenue: {chargeRecords.Sum(r => r.TotalCharge):C2}");
Console.WriteLine($"Average session duration: {TimeSpan.FromTicks((long)chargeRecords.Average(r => r.ParkingTime.Ticks)).TotalMinutes:F1} minutes");
Console.WriteLine($"Average energy per session: {chargeRecords.Average(r => r.KwhUsage):F2} kWh");
Console.WriteLine($"Average cost per session: {chargeRecords.Average(r => (double)r.TotalCharge):C2}");

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
