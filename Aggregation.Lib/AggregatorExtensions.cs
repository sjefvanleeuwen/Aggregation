namespace Aggregation.Lib;

/// <summary>
/// Extension methods for aggregation operations
/// </summary>
public static class AggregatorExtensions
{
    /// <summary>
    /// Aggregate a collection of items by a time period
    /// </summary>
    public static IDictionary<DateTime, T> AggregateByPeriod<T>(
        this IEnumerable<T> items,
        Func<T, DateTime> dateSelector,
        AggregationPeriod period) where T : class, new()
    {
        var result = new Dictionary<DateTime, T>();
        var groupedItems = items.GroupBy(item => GetStartDate(dateSelector(item), period));
        
        foreach (var group in groupedItems)
        {
            var aggregator = new Aggregator<T>();
            aggregator.AddRange(group);
            result.Add(group.Key, aggregator.GetAggregatedResult());
        }
        
        return result;
    }
    
    /// <summary>
    /// Get the start date for a specific period
    /// </summary>
    private static DateTime GetStartDate(DateTime date, AggregationPeriod period)
    {
        return period switch
        {
            AggregationPeriod.Daily => date.Date,
            AggregationPeriod.Weekly => date.AddDays(-(int)date.DayOfWeek).Date,
            AggregationPeriod.Monthly => new DateTime(date.Year, date.Month, 1),
            AggregationPeriod.Yearly => new DateTime(date.Year, 1, 1),
            _ => date.Date
        };
    }
}
