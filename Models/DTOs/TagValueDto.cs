namespace PIWebAPIApp.Models.DTOs
{
    public class TagValueDto
    {
        public string Value { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public bool Good { get; set; }
        public string? UnitsAbbreviation { get; set; }
        public string DisplayValue { get; set; } = string.Empty;
    }
}