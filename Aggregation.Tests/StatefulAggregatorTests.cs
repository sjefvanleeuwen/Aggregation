using Xunit;
using Aggregation.Lib;

namespace Aggregation.Tests;

public class StatefulAggregatorTests
{
    // Sample test data spanning different time periods
    private List<EnergyReading> GetSampleReadings()
    {
        return new List<EnergyReading>
        {
            // January (Month 1)
            // Week 1
            new() { Timestamp = new DateTime(2023, 1, 1), KwH = 10.5, Voltage = 220.1, MeterId = "Meter1", Temperature = 22.5 },
            new() { Timestamp = new DateTime(2023, 1, 2), KwH = 12.7, Voltage = 220.3, MeterId = "Meter1", Temperature = 23.1 },
            
            // Week 2
            new() { Timestamp = new DateTime(2023, 1, 8), KwH = 11.3, Voltage = 219.7, MeterId = "Meter1", Temperature = 21.5 },
            new() { Timestamp = new DateTime(2023, 1, 9), KwH = 9.8, Voltage = 220.0, MeterId = "Meter1", Temperature = 22.0 },
            
            // February (Month 2)
            // Week 6
            new() { Timestamp = new DateTime(2023, 2, 5), KwH = 10.2, Voltage = 220.2, MeterId = "Meter1", Temperature = 21.8 },
            new() { Timestamp = new DateTime(2023, 2, 6), KwH = 11.5, Voltage = 219.9, MeterId = "Meter1", Temperature = 22.2 },
        };
    }

    [Fact]
    public void Add_UpdatesAffectedTimePeriodsCorrectly()
    {
        // Arrange
        var aggregator = new StatefulAggregator<EnergyReading>(r => r.Timestamp);
        var readings = GetSampleReadings();
        
        // Add initial data
        aggregator.AddRange(readings);
        
        // Verify initial aggregates
        var jan1 = new DateTime(2023, 1, 1);
        var jan2 = new DateTime(2023, 1, 2);
        var dailyJan1 = aggregator.GetDailyAggregate(jan1);
        var dailyJan2 = aggregator.GetDailyAggregate(jan2);
        
        // Assert with null check
        Assert.NotNull(dailyJan1);
        Assert.NotNull(dailyJan2);
        Assert.Equal(10.5, dailyJan1.KwH);
        Assert.Equal(12.7, dailyJan2.KwH);
        
        // Weekly aggregate for week containing Jan 1
        var weeklyJan1 = aggregator.GetWeeklyAggregate(jan1);
        Assert.NotNull(weeklyJan1);
        Assert.Equal(23.2, weeklyJan1.KwH, precision: 0); // Sum of Jan 1 and Jan 2
        
        // Monthly aggregate for January
        var monthlyJan = aggregator.GetMonthlyAggregate(jan1);
        Assert.NotNull(monthlyJan);
        Assert.Equal(44.3, monthlyJan.KwH, precision:0); // Sum of all January readings
        
        // Yearly aggregate for 2023
        var yearly2023 = aggregator.GetYearlyAggregate(jan1);
        Assert.NotNull(yearly2023);
        Assert.Equal(66.0, yearly2023.KwH, precision: 0); // Sum of all 2023 readings
        
        // Act - Add a new reading on Jan 1
        var newReading = new EnergyReading { 
            Timestamp = new DateTime(2023, 1, 1), 
            KwH = 5.5, 
            Voltage = 220.5, 
            MeterId = "Meter2",
            Temperature = 22.8 
        };
        
        aggregator.Add(newReading);
        
        // Assert - Verify that all affected aggregates are updated
        var updatedDailyJan1 = aggregator.GetDailyAggregate(jan1);
        var updatedWeeklyJan1 = aggregator.GetWeeklyAggregate(jan1);
        var updatedMonthlyJan = aggregator.GetMonthlyAggregate(jan1);
        var updatedYearly2023 = aggregator.GetYearlyAggregate(jan1);
        
        // Add null checks
        Assert.NotNull(updatedDailyJan1);
        Assert.NotNull(updatedWeeklyJan1);
        Assert.NotNull(updatedMonthlyJan);
        Assert.NotNull(updatedYearly2023);
        
        // Daily should include the new reading
        Assert.Equal(16.0, updatedDailyJan1.KwH, precision: 0); // 10.5 + 5.5
        
        // Weekly should be updated
        Assert.Equal(28.7, updatedWeeklyJan1.KwH, precision: 0); // 10.5 + 5.5 + 12.7
        
        // Monthly should be updated
        Assert.Equal(49.8, updatedMonthlyJan.KwH, precision: 0); // 44.3 + 5.5
        
        // Yearly should be updated
        Assert.Equal(71.5, updatedYearly2023.KwH, precision: 0); // 66.0 + 5.5
        
        // But February aggregates should be unchanged
        var feb5 = new DateTime(2023, 2, 5);
        var dailyFeb5 = aggregator.GetDailyAggregate(feb5);
        Assert.NotNull(dailyFeb5);
        Assert.Equal(10.2, dailyFeb5.KwH);
    }
    
    [Fact]
    public void GetAllAggregates_ReturnsCorrectCollections()
    {
        // Arrange
        var aggregator = new StatefulAggregator<EnergyReading>(r => r.Timestamp);
        var readings = GetSampleReadings();
        
        // Act
        aggregator.AddRange(readings);
        
        // Assert
        var dailyAggregates = aggregator.GetAllDailyAggregates();
        var weeklyAggregates = aggregator.GetAllWeeklyAggregates();
        var monthlyAggregates = aggregator.GetAllMonthlyAggregates();
        var yearlyAggregates = aggregator.GetAllYearlyAggregates();
        
        Assert.Equal(6, dailyAggregates.Count); // 6 unique days
        Assert.Equal(3, weeklyAggregates.Count); // 3 unique weeks
        Assert.Equal(2, monthlyAggregates.Count); // 2 unique months (Jan, Feb)
        Assert.Single(yearlyAggregates); // 1 unique year (2023)
    }
    
    [Fact]
    public void CustomConfiguration_AffectsAllAggregationLevels()
    {
        // Arrange
        var config = new AggregationConfiguration<EnergyReading>()
            .Property(r => r.KwH, AggregationMethod.Sum)
            .Property(r => r.Voltage, AggregationMethod.Average)
            .Property(r => r.Temperature, AggregationMethod.Min);
            
        var aggregator = new StatefulAggregator<EnergyReading>(
            r => r.Timestamp, 
            config);
            
        var readings = GetSampleReadings();
        
        // Act
        aggregator.AddRange(readings);
        
        // Assert - check that custom configuration is applied at all levels
        var jan1 = new DateTime(2023, 1, 1);
        
        var monthlyJan = aggregator.GetMonthlyAggregate(jan1);
        Assert.NotNull(monthlyJan);
        Assert.Equal(44.3, monthlyJan.KwH, precision: 0); // Sum
        Assert.Equal(220.025, monthlyJan.Voltage, precision: 0); // Average
        Assert.Equal(21.5, monthlyJan.Temperature); // Min
        
        var yearlyAggregate = aggregator.GetYearlyAggregate(jan1);
        Assert.NotNull(yearlyAggregate);
        Assert.Equal(66.0, yearlyAggregate.KwH, precision: 0); // Sum
        Assert.Equal(220.033, yearlyAggregate.Voltage, precision: 0); // Average
        Assert.Equal(21.5, yearlyAggregate.Temperature); // Min (across all readings)
    }
}
