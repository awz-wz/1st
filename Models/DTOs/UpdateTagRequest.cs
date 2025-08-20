namespace PIWebAPIApp.Models.DTOs
{
    public class UpdateTagRequest
    {
        public string? TagName { get; set; }
        public string? NewState { get; set; }
        public string? Email { get; set; }
        public string? Justification { get; set; }
        public string? User { get; set; }
    }
}