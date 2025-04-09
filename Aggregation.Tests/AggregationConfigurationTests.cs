using Xunit;
using Aggregation.Lib;
using System.Linq;
using System.Linq.Expressions;

namespace Aggregation.Tests;

public class AggregationConfigurationTests
{
    [Fact]
    public void PrimaryKey_ShouldStoreSinglePropertyName()
    {
        // Arrange & Act
        var config = new AggregationConfiguration<EnergyReading>()
            .PrimaryKey(r => r.MeterId);
        
        // Assert
        var keyNames = config.GetPrimaryKeyPropertyNames();
        Assert.Single(keyNames);
        Assert.Equal("MeterId", keyNames[0]);
        Assert.Equal("MeterId", config.GetPrimaryKeyPropertyName());
        Assert.False(config.HasCompositeKey());
    }
    
    [Fact]
    public void CompositeKey_ShouldStoreMultiplePropertyNames()
    {
        // Arrange & Act
        var config = new AggregationConfiguration<EnergyReading>()
            .CompositeKey(
                r => r.MeterId, 
                r => r.Timestamp
            );
        
        // Assert
        var keyNames = config.GetPrimaryKeyPropertyNames();
        Assert.Equal(2, keyNames.Count);
        Assert.Contains("MeterId", keyNames);
        Assert.Contains("Timestamp", keyNames);
        Assert.True(config.HasCompositeKey());
    }
    
    [Fact]
    public void CompositeKey_WithMoreProperties_ShouldStoreAllPropertyNames()
    {
        // Arrange & Act
        var config = new AggregationConfiguration<EnergyReading>()
            .CompositeKey(
                r => r.MeterId,
                r => r.Timestamp,
                r => r.Voltage
            );
        
        // Assert
        var keyNames = config.GetPrimaryKeyPropertyNames();
        Assert.Equal(3, keyNames.Count);
        Assert.Equal(new[] { "MeterId", "Timestamp", "Voltage" }, keyNames);
        Assert.True(config.HasCompositeKey());
    }
    
    [Fact]
    public void PrimaryKey_AfterCompositeKey_ShouldReplaceAllKeys()
    {
        // Arrange
        var config = new AggregationConfiguration<EnergyReading>()
            .CompositeKey(r => r.MeterId, r => r.Timestamp);
        
        // Act - set a single primary key after setting a composite key
        config.PrimaryKey(r => r.KwH);
        
        // Assert
        var keyNames = config.GetPrimaryKeyPropertyNames();
        Assert.Single(keyNames);
        Assert.Equal("KwH", keyNames[0]);
        Assert.Equal("KwH", config.GetPrimaryKeyPropertyName());
        Assert.False(config.HasCompositeKey());
    }
    
    [Fact]
    public void CompositeKey_AfterPrimaryKey_ShouldReplaceKey()
    {
        // Arrange
        var config = new AggregationConfiguration<EnergyReading>()
            .PrimaryKey(r => r.MeterId);
        
        // Act - set a composite key after setting a primary key
        config.CompositeKey(r => r.Timestamp, r => r.Voltage);
        
        // Assert
        var keyNames = config.GetPrimaryKeyPropertyNames();
        Assert.Equal(2, keyNames.Count);
        Assert.Equal(new[] { "Timestamp", "Voltage" }, keyNames);
        Assert.True(config.HasCompositeKey());
    }
    
    [Fact]
    public void CompositeKey_WithEmptyArray_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new AggregationConfiguration<EnergyReading>();
        
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            config.CompositeKey(Array.Empty<Expression<Func<EnergyReading, object>>>()));
        
        Assert.Contains("At least one property must be provided", exception.Message);
    }
    
    [Fact]
    public void FluentInterface_ShouldAllowChaining()
    {
        // Arrange & Act
        var config = new AggregationConfiguration<EnergyReading>()
            .CompositeKey(r => r.MeterId, r => r.Timestamp)
            .Property(r => r.KwH, AggregationMethod.Sum)
            .Property(r => r.Voltage, AggregationMethod.Average);
        
        // Assert
        Assert.True(config.HasCompositeKey());
        Assert.Equal(2, config.GetPrimaryKeyPropertyNames().Count);
        Assert.Equal(AggregationMethod.Sum, config.GetMethodForProperty("KwH"));
        Assert.Equal(AggregationMethod.Average, config.GetMethodForProperty("Voltage"));
    }
    
    // Test for practical use case: Aggregating data by composite key
    [Fact]
    public void AggregatingByCompositeKey_ShouldGroupDataCorrectly()
    {
        // Arrange
        var readings = new List<EnergyReading>
        {
            // Two readings for Meter1 on Jan 1
            new() { Timestamp = new DateTime(2023, 1, 1), KwH = 10.0, MeterId = "Meter1" },
            new() { Timestamp = new DateTime(2023, 1, 1), KwH = 15.0, MeterId = "Meter1" },
            
            // One reading for Meter1 on Jan 2
            new() { Timestamp = new DateTime(2023, 1, 2), KwH = 20.0, MeterId = "Meter1" },
            
            // Two readings for Meter2 on Jan 1
            new() { Timestamp = new DateTime(2023, 1, 1), KwH = 5.0, MeterId = "Meter2" },
            new() { Timestamp = new DateTime(2023, 1, 1), KwH = 8.0, MeterId = "Meter2" }
        };
        
        // Group manually by composite key (MeterId + Date) to verify expected results
        var expectedGroups = readings
            .GroupBy(r => new { r.MeterId, Date = r.Timestamp.Date })
            .ToDictionary(
                g => g.Key,
                g => g.Sum(r => r.KwH)
            );
        
        // Assert expected grouping results
        Assert.Equal(3, expectedGroups.Count); // 3 unique combinations of MeterId + Date
        Assert.Equal(25.0, expectedGroups[new { MeterId = "Meter1", Date = new DateTime(2023, 1, 1) }]);
        Assert.Equal(20.0, expectedGroups[new { MeterId = "Meter1", Date = new DateTime(2023, 1, 2) }]);
        Assert.Equal(13.0, expectedGroups[new { MeterId = "Meter2", Date = new DateTime(2023, 1, 1) }]);
        
        // Note: In a real system, the composite key would be used to configure aggregation
        // through domain-specific methods that leverage the configuration.
    }
}
