using System.Collections.Concurrent;

namespace Aggregation.Lib;

/// <summary>
/// A stateful aggregator that maintains hierarchical aggregates at different time periods.
/// When data is added at a lower level (e.g., day), all higher levels (week, month, year)
/// containing that time period are automatically updated.
/// </summary>
/// <typeparam name="T">The type of POCO objects to aggregate</typeparam>
public class StatefulAggregator<T> where T : class, new()
{
    // Store aggregates for each time period
    private readonly ConcurrentDictionary<DateTime, T> _dailyAggregates = new();
    private readonly ConcurrentDictionary<DateTime, T> _weeklyAggregates = new();
    private readonly ConcurrentDictionary<DateTime, T> _monthlyAggregates = new();
    private readonly ConcurrentDictionary<DateTime, T> _yearlyAggregates = new();
    
    // Raw data storage by day for efficient recalculation
    private readonly ConcurrentDictionary<DateTime, List<T>> _rawDataByDay = new();
    
    // Configuration to use for aggregation
    private readonly AggregationConfiguration<T> _configuration;
    
    // Function to extract timestamp from an item
    private readonly Func<T, DateTime> _dateSelector;
    
    /// <summary>
    /// Creates a new stateful aggregator with the specified date selector and configuration
    /// </summary>
    /// <param name="dateSelector">Function to extract the date/time from each item</param>
    /// <param name="configuration">Optional custom aggregation configuration</param>
    public StatefulAggregator(Func<T, DateTime> dateSelector, AggregationConfiguration<T>? configuration = null)
    {
        _dateSelector = dateSelector ?? throw new ArgumentNullException(nameof(dateSelector));
        _configuration = configuration ?? new AggregationConfiguration<T>();
    }
    
    /// <summary>
    /// Adds a single item and updates all affected time period aggregates
    /// </summary>
    public void Add(T item)
    {
        if (item == null) return;
        
        var date = _dateSelector(item).Date;
        
        // Add to raw data
        _rawDataByDay.AddOrUpdate(
            date,
            new List<T> { item },
            (_, existing) => { existing.Add(item); return existing; }
        );
        
        // Update affected aggregates
        UpdateAffectedAggregates(date);
    }
    
    /// <summary>
    /// Adds multiple items and updates all affected time period aggregates
    /// </summary>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null) return;
        
        // Group items by date for efficient updates
        var itemsByDate = items
            .Where(item => item != null)
            .GroupBy(item => _dateSelector(item).Date);
        
        foreach (var group in itemsByDate)
        {
            var date = group.Key;
            
            // Add to raw data
            _rawDataByDay.AddOrUpdate(
                date,
                new List<T>(group),
                (_, existing) => { existing.AddRange(group); return existing; }
            );
            
            // Update affected aggregates
            UpdateAffectedAggregates(date);
        }
    }
    
    /// <summary>
    /// Updates all aggregates that are affected by adding data on the specified date
    /// </summary>
    private void UpdateAffectedAggregates(DateTime date)
    {
        UpdateDailyAggregate(date);
        UpdateWeeklyAggregate(date);
        UpdateMonthlyAggregate(date);
        UpdateYearlyAggregate(date);
    }
    
    /// <summary>
    /// Gets the daily aggregate for the specified date
    /// </summary>
    public T? GetDailyAggregate(DateTime date)
    {
        date = date.Date;
        return _dailyAggregates.TryGetValue(date, out var result) ? result : null;
    }
    
    /// <summary>
    /// Gets the weekly aggregate containing the specified date
    /// </summary>
    public T? GetWeeklyAggregate(DateTime date)
    {
        var weekStart = GetWeekStart(date);
        return _weeklyAggregates.TryGetValue(weekStart, out var result) ? result : null;
    }
    
    /// <summary>
    /// Gets the monthly aggregate containing the specified date
    /// </summary>
    public T? GetMonthlyAggregate(DateTime date)
    {
        var monthStart = GetMonthStart(date);
        return _monthlyAggregates.TryGetValue(monthStart, out var result) ? result : null;
    }
    
    /// <summary>
    /// Gets the yearly aggregate containing the specified date
    /// </summary>
    public T? GetYearlyAggregate(DateTime date)
    {
        var yearStart = GetYearStart(date);
        return _yearlyAggregates.TryGetValue(yearStart, out var result) ? result : null;
    }
    
    /// <summary>
    /// Gets all daily aggregates
    /// </summary>
    public IDictionary<DateTime, T> GetAllDailyAggregates() => 
        new Dictionary<DateTime, T>(_dailyAggregates);
    
    /// <summary>
    /// Gets all weekly aggregates
    /// </summary>
    public IDictionary<DateTime, T> GetAllWeeklyAggregates() => 
        new Dictionary<DateTime, T>(_weeklyAggregates);
    
    /// <summary>
    /// Gets all monthly aggregates
    /// </summary>
    public IDictionary<DateTime, T> GetAllMonthlyAggregates() => 
        new Dictionary<DateTime, T>(_monthlyAggregates);
    
    /// <summary>
    /// Gets all yearly aggregates
    /// </summary>
    public IDictionary<DateTime, T> GetAllYearlyAggregates() => 
        new Dictionary<DateTime, T>(_yearlyAggregates);
    
    /// <summary>
    /// Updates the daily aggregate for the specified date
    /// </summary>
    private void UpdateDailyAggregate(DateTime date)
    {
        date = date.Date;
        if (_rawDataByDay.TryGetValue(date, out var items))
        {
            var aggregator = new Aggregator<T>(_configuration);
            aggregator.AddRange(items);
            _dailyAggregates[date] = aggregator.GetAggregatedResult();
        }
    }
    
    /// <summary>
    /// Updates the weekly aggregate containing the specified date
    /// </summary>
    private void UpdateWeeklyAggregate(DateTime date)
    {
        var weekStart = GetWeekStart(date);
        var weekEnd = weekStart.AddDays(7);
        
        // Find all days in this week that have data
        var daysInWeek = _rawDataByDay.Keys
            .Where(d => d >= weekStart && d < weekEnd)
            .ToList();
        
        if (daysInWeek.Any())
        {
            var itemsInWeek = daysInWeek
                .SelectMany(day => _rawDataByDay[day])
                .ToList();
            
            var aggregator = new Aggregator<T>(_configuration);
            aggregator.AddRange(itemsInWeek);
            _weeklyAggregates[weekStart] = aggregator.GetAggregatedResult();
        }
    }
    
    /// <summary>
    /// Updates the monthly aggregate containing the specified date
    /// </summary>
    private void UpdateMonthlyAggregate(DateTime date)
    {
        var monthStart = GetMonthStart(date);
        var monthEnd = monthStart.AddMonths(1);
        
        // Find all days in this month that have data
        var daysInMonth = _rawDataByDay.Keys
            .Where(d => d >= monthStart && d < monthEnd)
            .ToList();
        
        if (daysInMonth.Any())
        {
            var itemsInMonth = daysInMonth
                .SelectMany(day => _rawDataByDay[day])
                .ToList();
            
            var aggregator = new Aggregator<T>(_configuration);
            aggregator.AddRange(itemsInMonth);
            _monthlyAggregates[monthStart] = aggregator.GetAggregatedResult();
        }
    }
    
    /// <summary>
    /// Updates the yearly aggregate containing the specified date
    /// </summary>
    private void UpdateYearlyAggregate(DateTime date)
    {
        var yearStart = GetYearStart(date);
        var yearEnd = yearStart.AddYears(1);
        
        // Find all days in this year that have data
        var daysInYear = _rawDataByDay.Keys
            .Where(d => d >= yearStart && d < yearEnd)
            .ToList();
        
        if (daysInYear.Any())
        {
            var itemsInYear = daysInYear
                .SelectMany(day => _rawDataByDay[day])
                .ToList();
            
            var aggregator = new Aggregator<T>(_configuration);
            aggregator.AddRange(itemsInYear);
            _yearlyAggregates[yearStart] = aggregator.GetAggregatedResult();
        }
    }
    
    // Helper methods to get period start dates
    private static DateTime GetWeekStart(DateTime date) => 
        date.AddDays(-(int)date.DayOfWeek).Date;
    
    private static DateTime GetMonthStart(DateTime date) => 
        new DateTime(date.Year, date.Month, 1);
    
    private static DateTime GetYearStart(DateTime date) => 
        new DateTime(date.Year, 1, 1);
}
