using Xunit;
using Aggregation.Lib;

namespace Aggregation.Tests;

public class AggregationTests
{
    // Sample test data
    private List<EnergyReading> GetSampleReadings()
    {
        return new List<EnergyReading>
        {
            new() { Timestamp = new DateTime(2023, 1, 1), KwH = 10.5, Voltage = 220.1, MeterId = "Meter1", Temperature = 22.5 },
            new() { Timestamp = new DateTime(2023, 1, 1), KwH = 8.2, Voltage = 219.8, MeterId = "Meter2", Temperature = 21.8 },
            new() { Timestamp = new DateTime(2023, 1, 2), KwH = 12.7, Voltage = 220.3, MeterId = "Meter1", Temperature = 23.1 },
            new() { Timestamp = new DateTime(2023, 1, 2), KwH = 9.1, Voltage = 220.0, MeterId = "Meter2", Temperature = 22.0 },
            new() { Timestamp = new DateTime(2023, 1, 8), KwH = 11.3, Voltage = 219.7, MeterId = "Meter1", Temperature = 21.5 },
            new() { Timestamp = new DateTime(2023, 1, 8), KwH = 8.9, Voltage = 220.2, MeterId = "Meter2", Temperature = 22.3 }
        };
    }

    [Fact]
    public void DefaultAggregation_ShouldSumAllNumericProperties()
    {
        // Arrange
        var readings = GetSampleReadings();
        var defaultAggregator = new Aggregator<EnergyReading>();
        
        // Act
        defaultAggregator.AddRange(readings);
        var result = defaultAggregator.GetAggregatedResult();
        
        // Assert
        Assert.Equal(60.7, result.KwH, 3); // Sum of all KwH values, precision 3 decimal places
        Assert.Equal(6, result.ReadingCount); // Sum of all ReadingCount values (default is 1 per reading)
    }

    [Fact]
    public void CustomConfiguration_ShouldApplySpecifiedAggregationMethods()
    {
        // Arrange
        var readings = GetSampleReadings();
        var config = new AggregationConfiguration<EnergyReading>()
            .Property(r => r.KwH, AggregationMethod.Sum)
            .Property(r => r.Voltage, AggregationMethod.Average)
            .Property(r => r.Temperature, AggregationMethod.Min)
            .Property(r => r.ReadingCount, AggregationMethod.Count);
        
        var aggregator = new Aggregator<EnergyReading>(config);
        
        // Act
        aggregator.AddRange(readings);
        var result = aggregator.GetAggregatedResult();
        
        // Assert
        Assert.Equal(60.7, result.KwH, 3); // Sum
        Assert.Equal(220.0167, result.Voltage, 4); // Average, precision 4 decimal places
        Assert.Equal(21.5, result.Temperature); // Min
        Assert.Equal(6, result.ReadingCount); // Count
    }

    [Fact]
    public void CustomAggregationFunction_ShouldApplyCustomLogic()
    {
        // Arrange
        var readings = GetSampleReadings();
        
        // Define custom aggregator with explicit typing
        Func<IEnumerable<double>, double> customVoltageAggregator = values => 
            values.Where(v => v >= 220.0).DefaultIfEmpty().Average();
            
        var customConfig = new AggregationConfiguration<EnergyReading>()
            .Property(r => r.KwH, AggregationMethod.Sum)
            .Property(r => r.Voltage, customVoltageAggregator);
        
        var aggregator = new Aggregator<EnergyReading>(customConfig);
        
        // Act
        aggregator.AddRange(readings);
        var result = aggregator.GetAggregatedResult();
        
        // Assert
        Assert.Equal(60.7, result.KwH, 3); // Sum
        Assert.Equal(220.15, result.Voltage, 2); // Average of only values >= 220.0
    }

    [Fact]
    public void GroupedPropertyConfiguration_ShouldApplySameMethodToMultipleProperties()
    {
        // Arrange
        var readings = GetSampleReadings();
        var groupedConfig = new AggregationConfiguration<EnergyReading>()
            .Properties(AggregationMethod.Sum, r => r.KwH, r => r.ReadingCount)
            .Properties(AggregationMethod.Average, r => r.Voltage, r => r.Temperature);
        
        var aggregator = new Aggregator<EnergyReading>(groupedConfig);
        
        // Act
        aggregator.AddRange(readings);
        var result = aggregator.GetAggregatedResult();
        
        // Assert
        Assert.Equal(60.7, result.KwH, 3); // Sum
        Assert.Equal(6, result.ReadingCount); // Sum
        Assert.Equal(220.0167, result.Voltage, 4); // Average
        Assert.Equal(22.2, result.Temperature, 1); // Average
    }

    [Fact]
    public void AllNumericPropertiesConfiguration_ShouldConfigureAllNumericProperties()
    {
        // Arrange
        var readings = GetSampleReadings();
        var allPropsConfig = new AggregationConfiguration<EnergyReading>()
            .AllNumericProperties(AggregationMethod.Sum)
            .Exclude(r => r.Voltage); // Except Voltage
        
        var aggregator = new Aggregator<EnergyReading>(allPropsConfig);
        
        // Act
        aggregator.AddRange(readings);
        var result = aggregator.GetAggregatedResult();
        
        // Assert
        Assert.Equal(60.7, result.KwH, 3); // Sum
        Assert.Equal(0, result.Voltage); // Excluded (default value)
        Assert.Equal(133.2, result.Temperature, 1); // Sum
        Assert.Equal(6, result.ReadingCount); // Sum
    }

    [Fact]
    public void PeriodBasedAggregation_ShouldGroupBySpecifiedPeriod()
    {
        // Arrange
        var readings = GetSampleReadings();
        
        // Act
        var dailyAggregations = readings.AggregateByPeriod(
            r => r.Timestamp, 
            AggregationPeriod.Daily);
        
        // Assert
        Assert.Equal(3, dailyAggregations.Count); // 3 distinct days
        
        // Check first day
        var day1 = new DateTime(2023, 1, 1);
        Assert.True(dailyAggregations.ContainsKey(day1));
        Assert.Equal(18.7, dailyAggregations[day1].KwH, 1); // Sum of KwH for first day
        
        // Check second day
        var day2 = new DateTime(2023, 1, 2);
        Assert.True(dailyAggregations.ContainsKey(day2));
        Assert.Equal(21.8, dailyAggregations[day2].KwH, 1); // Sum of KwH for second day
        
        // Check third day
        var day3 = new DateTime(2023, 1, 8);
        Assert.True(dailyAggregations.ContainsKey(day3));
        Assert.Equal(20.2, dailyAggregations[day3].KwH, 1); // Sum of KwH for third day
    }

    [Fact]
    public void WeeklyAggregation_ShouldGroupByWeek()
    {
        // Arrange
        var readings = GetSampleReadings();
        
        // Act
        var weeklyAggregations = readings.AggregateByPeriod(
            r => r.Timestamp, 
            AggregationPeriod.Weekly);
        
        // Assert
        Assert.Equal(2, weeklyAggregations.Count); // 2 distinct weeks
        
        // Data spans two weeks:
        // Week 1: Jan 1-2, 2023 (part of the first week)
        // Week 2: Jan 8, 2023 (part of the second week)
        
        var weekStarts = weeklyAggregations.Keys.OrderBy(d => d).ToList();
        
        // Check aggregation for first week
        Assert.Equal(40.5, weeklyAggregations[weekStarts[0]].KwH, 1); // Sum of KwH for first week
        
        // Check aggregation for second week
        Assert.Equal(20.2, weeklyAggregations[weekStarts[1]].KwH, 1); // Sum of KwH for second week
    }
}
