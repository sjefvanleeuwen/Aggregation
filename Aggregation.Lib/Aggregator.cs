using System.Reflection;

namespace Aggregation.Lib;

/// <summary>
/// Generic aggregator for POCO objects
/// </summary>
/// <typeparam name="T">The type of POCO objects to aggregate</typeparam>
public class Aggregator<T> where T : class, new()
{
    private readonly List<T> _items = new();
    private readonly Dictionary<string, PropertyInfo> _numericProperties;
    private readonly AggregationConfiguration<T> _configuration;
    
    /// <summary>
    /// Initialize a new aggregator with default configuration
    /// </summary>
    public Aggregator() : this(new AggregationConfiguration<T>())
    {
    }
    
    /// <summary>
    /// Initialize a new aggregator with custom configuration
    /// </summary>
    public Aggregator(AggregationConfiguration<T> configuration)
    {
        _configuration = configuration;
        
        // Find all numeric properties in the type T
        _numericProperties = typeof(T).GetProperties()
            .Where(p => IsNumericType(p.PropertyType))
            .ToDictionary(p => p.Name);
    }
    
    /// <summary>
    /// Add an item to be aggregated
    /// </summary>
    public void Add(T item)
    {
        if (item != null)
        {
            _items.Add(item);
        }
    }
    
    /// <summary>
    /// Add multiple items to be aggregated
    /// </summary>
    public void AddRange(IEnumerable<T> items)
    {
        if (items != null)
        {
            _items.AddRange(items.Where(i => i != null));
        }
    }
    
    /// <summary>
    /// Get the aggregated result
    /// </summary>
    public T GetAggregatedResult()
    {
        if (_items.Count == 0)
        {
            return new T();
        }
        
        var result = new T();
        
        foreach (var prop in _numericProperties.Values)
        {
            // Skip properties that have been explicitly excluded
            if (_configuration.IsPropertyExcluded(prop.Name))
            {
                continue;
            }
            
            var aggregatedValue = CalculateAggregatedValue(prop);
            if (aggregatedValue != null)
            {
                prop.SetValue(result, Convert.ChangeType(aggregatedValue, prop.PropertyType));
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Calculate the aggregated value for a property based on configuration
    /// </summary>
    private object? CalculateAggregatedValue(PropertyInfo property)
    {
        var propertyName = property.Name;
        var method = _configuration.GetMethodForProperty(propertyName);
        
        return method switch
        {
            AggregationMethod.Sum => CalculateSum(property),
            AggregationMethod.Min => CalculateMin(property),
            AggregationMethod.Max => CalculateMax(property),
            AggregationMethod.Average => CalculateAverage(property),
            AggregationMethod.First => CalculateFirst(property),
            AggregationMethod.Last => CalculateLast(property),
            AggregationMethod.Count => _items.Count,
            AggregationMethod.Custom => CalculateCustom(property),
            _ => CalculateSum(property)
        };
    }
    
    /// <summary>
    /// Calculate the sum of a property across all items
    /// </summary>
    private object CalculateSum(PropertyInfo property)
    {
        var type = property.PropertyType;
        
        if (type == typeof(int))
        {
            return _items.Sum(item => (int?)property.GetValue(item) ?? 0);
        }
        else if (type == typeof(long))
        {
            return _items.Sum(item => (long?)property.GetValue(item) ?? 0);
        }
        else if (type == typeof(float))
        {
            return _items.Sum(item => (float?)property.GetValue(item) ?? 0);
        }
        else if (type == typeof(double))
        {
            return _items.Sum(item => (double?)property.GetValue(item) ?? 0);
        }
        else if (type == typeof(decimal))
        {
            return _items.Sum(item => (decimal?)property.GetValue(item) ?? 0);
        }
        
        return 0;
    }
    
    /// <summary>
    /// Calculate the minimum value of a property
    /// </summary>
    private object? CalculateMin(PropertyInfo property)
    {
        if (_items.Count == 0) return null;
        
        var type = property.PropertyType;
        
        if (type == typeof(int))
        {
            return _items.Min(item => (int?)property.GetValue(item) ?? int.MaxValue);
        }
        else if (type == typeof(long))
        {
            return _items.Min(item => (long?)property.GetValue(item) ?? long.MaxValue);
        }
        else if (type == typeof(float))
        {
            return _items.Min(item => (float?)property.GetValue(item) ?? float.MaxValue);
        }
        else if (type == typeof(double))
        {
            return _items.Min(item => (double?)property.GetValue(item) ?? double.MaxValue);
        }
        else if (type == typeof(decimal))
        {
            return _items.Min(item => (decimal?)property.GetValue(item) ?? decimal.MaxValue);
        }
        
        return null;
    }
    
    /// <summary>
    /// Calculate the maximum value of a property
    /// </summary>
    private object? CalculateMax(PropertyInfo property)
    {
        if (_items.Count == 0) return null;
        
        var type = property.PropertyType;
        
        if (type == typeof(int))
        {
            return _items.Max(item => (int?)property.GetValue(item) ?? int.MinValue);
        }
        else if (type == typeof(long))
        {
            return _items.Max(item => (long?)property.GetValue(item) ?? long.MinValue);
        }
        else if (type == typeof(float))
        {
            return _items.Max(item => (float?)property.GetValue(item) ?? float.MinValue);
        }
        else if (type == typeof(double))
        {
            return _items.Max(item => (double?)property.GetValue(item) ?? double.MinValue);
        }
        else if (type == typeof(decimal))
        {
            return _items.Max(item => (decimal?)property.GetValue(item) ?? decimal.MinValue);
        }
        
        return null;
    }
    
    /// <summary>
    /// Calculate the average value of a property
    /// </summary>
    private object? CalculateAverage(PropertyInfo property)
    {
        if (_items.Count == 0) return null;
        
        var type = property.PropertyType;
        
        if (type == typeof(int))
        {
            return _items.Average(item => (int?)property.GetValue(item) ?? 0);
        }
        else if (type == typeof(long))
        {
            return (decimal)_items.Average(item => (long?)property.GetValue(item) ?? 0);
        }
        else if (type == typeof(float))
        {
            return _items.Average(item => (float?)property.GetValue(item) ?? 0);
        }
        else if (type == typeof(double))
        {
            return _items.Average(item => (double?)property.GetValue(item) ?? 0);
        }
        else if (type == typeof(decimal))
        {
            return _items.Average(item => (decimal?)property.GetValue(item) ?? 0);
        }
        
        return null;
    }
    
    /// <summary>
    /// Get the first value of a property
    /// </summary>
    private object? CalculateFirst(PropertyInfo property)
    {
        if (_items.Count == 0) return null;
        return property.GetValue(_items.First());
    }
    
    /// <summary>
    /// Get the last value of a property
    /// </summary>
    private object? CalculateLast(PropertyInfo property)
    {
        if (_items.Count == 0) return null;
        return property.GetValue(_items.Last());
    }
    
    /// <summary>
    /// Apply a custom aggregation function to a property
    /// </summary>
    private object? CalculateCustom(PropertyInfo property)
    {
        var customAggregator = _configuration.GetCustomAggregatorForProperty(property.Name);
        if (customAggregator == null) return null;
        
        // Get property values and convert them to the correct type
        var propertyType = property.PropertyType;
        var values = _items.Select(item => property.GetValue(item));
        
        // Create a typed enumerable
        var typedEnumerableType = typeof(IEnumerable<>).MakeGenericType(propertyType);
        
        // Use reflection to cast the collection to the correct type
        var castMethod = typeof(Enumerable)
            .GetMethod("Cast")
            ?.MakeGenericMethod(propertyType);
        
        if (castMethod == null) return null;
        
        var typedValues = castMethod.Invoke(null, new object[] { values });
        if (typedValues == null) return null;
        
        // Invoke the custom aggregator with the correctly typed values
        var method = customAggregator.GetType().GetMethod("Invoke");
        if (method == null) return null;
        
        return method.Invoke(customAggregator, new object[] { typedValues });
    }
    
    /// <summary>
    /// Check if a type is numeric
    /// </summary>
    private bool IsNumericType(Type type)
    {
        return type == typeof(int) || 
               type == typeof(long) || 
               type == typeof(float) || 
               type == typeof(double) || 
               type == typeof(decimal);
    }
}
