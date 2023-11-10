namespace HotChocolatier.Adnotations
{

    public class GraphQlListAttribute : System.Attribute
    {
        public bool UseProjection { get; set; }
        public bool UseSorting { get; set; }
        public bool UseFiltering { get; set; }
        public bool Authorize { get; set; }
    }
}

