namespace HotChocolatier.Adnotations;

public class GraphQlPagedListAttribute : System.Attribute
{
    public bool UseProjection { get; set; }
    public bool UseSorting { get; set; }
    public bool UseFiltering { get; set; }
    public bool Authorize { get; set; }
    public int DefaultPageSize { get; set; } = 50;
    public bool IncludeTotalCount { get; set; } = true;
    public int MaxPageSize { get; set; } = 1000;
}