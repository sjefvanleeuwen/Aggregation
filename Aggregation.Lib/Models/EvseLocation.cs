namespace Aggregation.Lib.Models;

public class EvseLocation
{
    public int LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public List<string> EvseIds { get; set; } = new List<string>();
}
