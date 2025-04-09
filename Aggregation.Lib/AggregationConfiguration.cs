using System.Linq.Expressions;
using System.Reflection;

namespace Aggregation.Lib;

/// <summary>
/// Defines the available aggregation methods for numeric properties
/// </summary>
public enum AggregationMethod
{
    Sum,
    Min,
    Max,
    Average,
    First,
    Last,
    Count,
    Custom
}

/// <summary>
/// Configuration for a property's aggregation method
/// </summary>
public class PropertyAggregationConfig
{
    public string PropertyName { get; set; } = string.Empty;
    public AggregationMethod Method { get; set; } = AggregationMethod.Sum;
    public Delegate? CustomAggregator { get; set; }
}

/// <summary>
/// Fluent configuration builder for the aggregator
/// </summary>
/// <typeparam name="T">The type of object being aggregated</typeparam>
public class AggregationConfiguration<T> where T : class, new()
{
    private readonly Dictionary<string, PropertyAggregationConfig> _configurations = new();
    private readonly HashSet<string> _excludedProperties = new();
    private readonly List<string> _primaryKeyPropertyNames = new();

    /// <summary>
    /// Specifies which property should be treated as the primary key
    /// </summary>
    public AggregationConfiguration<T> PrimaryKey<TProp>(Expression<Func<T, TProp>> propertySelector)
    {
        var propertyName = ExtractPropertyName(propertySelector);
        _primaryKeyPropertyNames.Clear(); // Clear existing keys
        _primaryKeyPropertyNames.Add(propertyName);
        return this;
    }

    /// <summary>
    /// Specifies multiple properties that together form a composite primary key
    /// </summary>
    public AggregationConfiguration<T> CompositeKey(params Expression<Func<T, object>>[] propertySelectors)
    {
        if (propertySelectors == null || propertySelectors.Length == 0)
        {
            throw new ArgumentException("At least one property must be provided for composite key", nameof(propertySelectors));
        }

        _primaryKeyPropertyNames.Clear(); // Clear existing keys

        foreach (var selector in propertySelectors)
        {
            var propertyName = ExtractPropertyName(selector);
            _primaryKeyPropertyNames.Add(propertyName);
        }

        return this;
    }

    /// <summary>
    /// Configure a property with a specific aggregation method
    /// </summary>
    public AggregationConfiguration<T> Property<TProp>(
        Expression<Func<T, TProp>> propertySelector, 
        AggregationMethod method)
    {
        var propertyName = ExtractPropertyName(propertySelector);
        _configurations[propertyName] = new PropertyAggregationConfig
        {
            PropertyName = propertyName,
            Method = method
        };
        return this;
    }

    /// <summary>
    /// Configure a property with a custom aggregation function
    /// </summary>
    public AggregationConfiguration<T> Property<TProp>(
        Expression<Func<T, TProp>> propertySelector,
        Func<IEnumerable<TProp>, TProp> customAggregator)
    {
        var propertyName = ExtractPropertyName(propertySelector);
        _configurations[propertyName] = new PropertyAggregationConfig
        {
            PropertyName = propertyName,
            Method = AggregationMethod.Custom,
            CustomAggregator = customAggregator
        };
        return this;
    }

    /// <summary>
    /// Configure multiple properties to use the same aggregation method
    /// </summary>
    public AggregationConfiguration<T> Properties(
        AggregationMethod method,
        params Expression<Func<T, object>>[] propertySelectors)
    {
        foreach (var selector in propertySelectors)
        {
            var propertyName = ExtractPropertyName(selector);
            _configurations[propertyName] = new PropertyAggregationConfig
            {
                PropertyName = propertyName,
                Method = method
            };
        }
        return this;
    }

    /// <summary>
    /// Configure all numeric properties to use the same aggregation method
    /// </summary>
    public AggregationConfiguration<T> AllNumericProperties(AggregationMethod method)
    {
        var numericProperties = typeof(T).GetProperties()
            .Where(p => IsNumericType(p.PropertyType));

        foreach (var prop in numericProperties)
        {
            _configurations[prop.Name] = new PropertyAggregationConfig
            {
                PropertyName = prop.Name,
                Method = method
            };
        }
        return this;
    }

    /// <summary>
    /// Exclude specific properties from aggregation
    /// </summary>
    public AggregationConfiguration<T> Exclude<TProp>(
        params Expression<Func<T, TProp>>[] propertySelectors)
    {
        foreach (var selector in propertySelectors)
        {
            var propertyName = ExtractPropertyName(selector);
            if (_configurations.ContainsKey(propertyName))
            {
                _configurations.Remove(propertyName);
            }
            _excludedProperties.Add(propertyName);
        }
        return this;
    }

    // Helper methods for internal use
    public AggregationMethod GetMethodForProperty(string propertyName)
    {
        return _configurations.TryGetValue(propertyName, out var config)
            ? config.Method
            : AggregationMethod.Sum; // Default to Sum
    }

    public Delegate? GetCustomAggregatorForProperty(string propertyName)
    {
        return _configurations.TryGetValue(propertyName, out var config)
            ? config.CustomAggregator
            : null;
    }

    public bool HasConfigurationForProperty(string propertyName)
    {
        return _configurations.ContainsKey(propertyName);
    }

    public bool IsPropertyExcluded(string propertyName)
    {
        return _excludedProperties.Contains(propertyName);
    }

    /// <summary>
    /// Gets the names of properties configured as primary key components
    /// </summary>
    public IReadOnlyList<string> GetPrimaryKeyPropertyNames()
    {
        return _primaryKeyPropertyNames;
    }

    /// <summary>
    /// Gets the name of the property configured as the primary key, if any.
    /// If a composite key is configured, returns the first property name.
    /// </summary>
    /// <remarks>
    /// This method is maintained for backward compatibility.
    /// For composite keys, use GetPrimaryKeyPropertyNames() instead.
    /// </remarks>
    public string? GetPrimaryKeyPropertyName()
    {
        return _primaryKeyPropertyNames.FirstOrDefault();
    }

    /// <summary>
    /// Checks if a composite key is configured (more than one primary key property)
    /// </summary>
    public bool HasCompositeKey()
    {
        return _primaryKeyPropertyNames.Count > 1;
    }

    private string ExtractPropertyName<TProp>(Expression<Func<T, TProp>> propertySelector)
    {
        if (propertySelector.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }
        else if (propertySelector.Body is UnaryExpression unaryExpression && 
                 unaryExpression.NodeType == ExpressionType.Convert &&
                 unaryExpression.Operand is MemberExpression operandMemberExpression)
        {
            // Handle boxing conversion (e.g., when a value type is implicitly converted to object)
            return operandMemberExpression.Member.Name;
        }
        
        throw new ArgumentException("Expression is not a property access expression", nameof(propertySelector));
    }

    private bool IsNumericType(Type type)
    {
        return type == typeof(int) ||
               type == typeof(long) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal);
    }
}
