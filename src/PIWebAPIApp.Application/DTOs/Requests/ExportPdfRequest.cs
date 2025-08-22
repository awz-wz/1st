namespace PIWebAPIApp.Models.DTOs
{
    public class ExportPdfRequest
    {
        public List<TagInfo>? Tags { get; set; }
        public string? User { get; set; }
        public string? Email { get; set; }
        public string? Justification { get; set; }
    }
}