# Aggregation Library

A flexible, high-performance library for aggregating data in .NET applications with support for hierarchical time-based aggregations.

## Overview

The Aggregation Library provides a powerful and extensible way to aggregate data from POCO (Plain Old CLR Objects) classes in .NET applications. It's designed to be flexible, allowing you to configure how different properties should be aggregated (sum, average, min, max, etc.) and supports both simple one-time aggregations and stateful time-based hierarchical aggregations.

## Key Features

- **Generic Aggregation**: Works with any POCO class containing numeric properties
- **Flexible Configuration**: Configure different aggregation methods per property
- **Custom Aggregation Functions**: Define your own aggregation logic for special cases
- **Time-Based Aggregation**: Group and aggregate data by day, week, month, or year
- **Stateful Aggregation**: Efficiently maintain hierarchical aggregates that update automatically
- **Fluent API**: Intuitive configuration interface for specifying aggregation behavior

## Basic Usage

### Simple Aggregation

```csharp
// Create a collection of items to aggregate
var readings = new List<EnergyReading>
{
    new() { Timestamp = DateTime.Today, KwH = 10.5, Voltage = 220.1 },
    new() { Timestamp = DateTime.Today, KwH = 8.2, Voltage = 219.8 }
};

// Create an aggregator with default settings (sum for all numeric properties)
var aggregator = new Aggregator<EnergyReading>();
aggregator.AddRange(readings);

// Get the aggregated result
var result = aggregator.GetAggregatedResult();
Console.WriteLine($"Total KwH: {result.KwH}"); // Output: Total KwH: 18.7
```

### Custom Aggregation Configuration

```csharp
// Create a custom configuration
var config = new AggregationConfiguration<EnergyReading>()
    .Property(r => r.KwH, AggregationMethod.Sum)           // Sum up KwH values
    .Property(r => r.Voltage, AggregationMethod.Average)   // Average the voltage
    .Property(r => r.Temperature, AggregationMethod.Min)   // Get minimum temperature
    .PrimaryKey(r => r.MeterId);                           // Specify primary key

// Create an aggregator with the custom configuration
var aggregator = new Aggregator<EnergyReading>(config);
aggregator.AddRange(readings);

// Get the aggregated result
var result = aggregator.GetAggregatedResult();
Console.WriteLine($"Sum of KwH: {result.KwH}");
Console.WriteLine($"Average Voltage: {result.Voltage}");
Console.WriteLine($"Min Temperature: {result.Temperature}");
```

### Time-Based Aggregation

```csharp
// Aggregate data by day
var dailyAggregations = readings.AggregateByPeriod(
    r => r.Timestamp,
    AggregationPeriod.Daily);

// Aggregate data by week
var weeklyAggregations = readings.AggregateByPeriod(
    r => r.Timestamp,
    AggregationPeriod.Weekly);

// Iterate through daily aggregations
foreach (var day in dailyAggregations)
{
    Console.WriteLine($"Day: {day.Key:yyyy-MM-dd}, Total KwH: {day.Value.KwH}");
}
```

## Stateful Hierarchical Aggregation

The `StatefulAggregator` maintains aggregates at different time levels (day, week, month, year) and automatically updates all affected levels when new data is added.

```csharp
// Create a stateful aggregator
var statefulAggregator = new StatefulAggregator<EnergyReading>(r => r.Timestamp);

// Add data and automatically update all time periods
statefulAggregator.AddRange(readings);

// Add a new reading - all affected time periods are automatically updated
statefulAggregator.Add(new EnergyReading { 
    Timestamp = DateTime.Today, 
    KwH = 5.5, 
    Voltage = 220.5 
});

// Get aggregates for different time periods
var dailyAggregate = statefulAggregator.GetDailyAggregate(DateTime.Today);
var weeklyAggregate = statefulAggregator.GetWeeklyAggregate(DateTime.Today);
var monthlyAggregate = statefulAggregator.GetMonthlyAggregate(DateTime.Today);
var yearlyAggregate = statefulAggregator.GetYearlyAggregate(DateTime.Today);

// Get all aggregates for a time period
var allDailyAggregates = statefulAggregator.GetAllDailyAggregates();
```

## Advanced Configuration

### Custom Aggregation Functions

```csharp
// Define a custom aggregation function for voltage
Func<IEnumerable<double>, double> customVoltageAggregator = values => 
    values.Where(v => v >= 220.0)  // Only consider voltage >= 220.0
          .DefaultIfEmpty()         // Handle empty collections
          .Average();               // Calculate average

// Configure with the custom function
var config = new AggregationConfiguration<EnergyReading>()
    .Property(r => r.KwH, AggregationMethod.Sum)
    .Property(r => r.Voltage, customVoltageAggregator);
```

### Group Properties by Aggregation Method

```csharp
// Configure multiple properties with the same method
var config = new AggregationConfiguration<EnergyReading>()
    .Properties(AggregationMethod.Sum, r => r.KwH, r => r.ReadingCount)
    .Properties(AggregationMethod.Average, r => r.Voltage, r => r.Temperature);
```

### Configure All Numeric Properties

```csharp
// Configure all numeric properties to use the same method
var config = new AggregationConfiguration<EnergyReading>()
    .AllNumericProperties(AggregationMethod.Sum)
    .Exclude(r => r.Voltage);  // Exclude specific properties
```

## Performance Considerations

- The library is designed for efficiency with large datasets
- The stateful aggregator maintains aggregates and only recalculates what's necessary
- For very large datasets, consider streaming or batching data into the aggregator

## Dependencies

- .NET 9.0 or later
- No external dependencies

## License

MIT
