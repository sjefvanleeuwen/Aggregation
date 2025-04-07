namespace Aggregation.Lib;

/// <summary>
/// Example POCO class with a numeric field that can be aggregated
/// </summary>
public class EnergyReading
{
    public DateTime Timestamp { get; set; }
    public double KwH { get; set; }
    public double Voltage { get; set; }
    public string? MeterId { get; set; }
    public double Temperature { get; set; }
    public int ReadingCount { get; set; } = 1;
}

/// <summary>
/// Example usage of the Aggregator
/// </summary>
public static class AggregationExample
{
    public static void DemonstrateAggregation()
    {
        // Create some sample data
        var readings = new List<EnergyReading>
        {
            new EnergyReading { Timestamp = new DateTime(2023, 1, 1), KwH = 10.5, Voltage = 220.1, MeterId = "Meter1", Temperature = 22.5 },
            new EnergyReading { Timestamp = new DateTime(2023, 1, 1), KwH = 8.2, Voltage = 219.8, MeterId = "Meter2", Temperature = 21.8 },
            new EnergyReading { Timestamp = new DateTime(2023, 1, 2), KwH = 12.7, Voltage = 220.3, MeterId = "Meter1", Temperature = 23.1 },
            new EnergyReading { Timestamp = new DateTime(2023, 1, 2), KwH = 9.1, Voltage = 220.0, MeterId = "Meter2", Temperature = 22.0 },
            new EnergyReading { Timestamp = new DateTime(2023, 1, 8), KwH = 11.3, Voltage = 219.7, MeterId = "Meter1", Temperature = 21.5 },
            new EnergyReading { Timestamp = new DateTime(2023, 1, 8), KwH = 8.9, Voltage = 220.2, MeterId = "Meter2", Temperature = 22.3 }
        };

        // Basic aggregation (defaults to Sum for all numeric properties)
        var defaultAggregator = new Aggregator<EnergyReading>();
        defaultAggregator.AddRange(readings);
        var defaultResult = defaultAggregator.GetAggregatedResult();
        Console.WriteLine($"Default Aggregation - Total KwH: {defaultResult.KwH}");

        // Custom configuration using fluent API
        var config = new AggregationConfiguration<EnergyReading>()
            .Property(r => r.KwH, AggregationMethod.Sum)           // Sum up KwH
            .Property(r => r.Voltage, AggregationMethod.Average)   // Average the voltage
            .Property(r => r.Temperature, AggregationMethod.Min)   // Get minimum temperature
            .Property(r => r.ReadingCount, AggregationMethod.Count); // Count the readings

        // Also supports custom aggregation functions
        var customConfig = new AggregationConfiguration<EnergyReading>()
            .Property(r => r.KwH, AggregationMethod.Sum)
            .Property(r => r.Voltage, values => 
                values.Where(v => v >= 220.0).DefaultIfEmpty().Average()) // Custom average of only values >= 220.0
            .Property(r => r.ReadingCount, AggregationMethod.Count);

        // Grouping multiple properties
        var groupedConfig = new AggregationConfiguration<EnergyReading>()
            .Properties(AggregationMethod.Sum, r => r.KwH, r => r.ReadingCount)
            .Properties(AggregationMethod.Average, r => r.Voltage, r => r.Temperature);

        // Apply all numeric properties with the same method
        var allPropsConfig = new AggregationConfiguration<EnergyReading>()
            .AllNumericProperties(AggregationMethod.Sum)
            .Exclude(r => r.Voltage); // Except Voltage

        // Create an aggregator with our custom configuration
        var configuredAggregator = new Aggregator<EnergyReading>(config);
        configuredAggregator.AddRange(readings);
        var configuredResult = configuredAggregator.GetAggregatedResult();

        Console.WriteLine("\nConfigured Aggregation Results:");
        Console.WriteLine($"Sum of KwH: {configuredResult.KwH}");
        Console.WriteLine($"Average Voltage: {configuredResult.Voltage}");
        Console.WriteLine($"Min Temperature: {configuredResult.Temperature}");
        Console.WriteLine($"Count of readings: {configuredResult.ReadingCount}");

        // Period-based aggregation still works with custom configurations
        var dailyAggregations = readings.AggregateByPeriod(r => r.Timestamp, AggregationPeriod.Daily);
        var weeklyAggregations = readings.AggregateByPeriod(r => r.Timestamp, AggregationPeriod.Weekly);

        Console.WriteLine("\nDaily aggregations:");
        foreach (var day in dailyAggregations)
        {
            Console.WriteLine($"Day: {day.Key:yyyy-MM-dd}, Total KwH: {day.Value.KwH}");
        }
    }
}
